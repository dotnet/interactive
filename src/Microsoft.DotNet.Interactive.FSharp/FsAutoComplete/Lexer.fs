namespace FsAutoComplete

open System
open FSharp.Compiler.Tokenization

type SymbolKind =
  | Ident
  | Operator
  | GenericTypeParameter
  | StaticallyResolvedTypeParameter
  | ActivePattern
  | Keyword
  | Dot
  | Other

type LexerSymbol =
  { Kind: SymbolKind
    Line: uint32
    LeftColumn: uint32
    RightColumn: uint32
    Text: string }

[<RequireQualifiedAccess>]
type SymbolLookupKind =
  | Fuzzy
  | ByLongIdent
  | Simple
  | ForCompletion

type private DraftToken =
  { Kind: SymbolKind
    Token: FSharpTokenInfo
    RightColumn: uint32 }

  static member inline Create kind token =
    { Kind = kind
      Token = token
      RightColumn = uint32 (token.LeftColumn + token.FullMatchedLength - 1) }

module Lexer =

  [<return: Struct>]
  let (|Define|_|) (a: string) =
    if a.StartsWith("--define:", StringComparison.Ordinal) then
      ValueSome(a.[9..])
    else
      ValueNone

  [<return: Struct>]
  let (|LangVersion|_|) (a: string) =
    if a.StartsWith("--langversion:", StringComparison.Ordinal) then
      ValueSome(a.[14..])
    else
      ValueNone

  /// Return all tokens of current line
  let tokenizeLine (args: string[]) lineStr =
    let defines, langVersion =
      ((ResizeArray(), None), args)
      ||> Array.fold (fun (defines, langVersion) arg ->
        match arg with
        | Define d ->
          defines.Add(d)
          defines, langVersion
        | LangVersion v -> defines, Some(v)
        | _ -> defines, langVersion)

    let sourceTokenizer =
      FSharpSourceTokenizer(Seq.toList defines, Some "/tmp.fsx", langVersion, None)

    let lineTokenizer = sourceTokenizer.CreateLineTokenizer lineStr

    let rec loop lexState acc =
      match lineTokenizer.ScanToken lexState with
      | Some tok, state -> loop state (tok :: acc)
      | _ -> List.rev acc

    loop FSharpTokenizerLexState.Initial []

  let inline private isIdentifier t = t.CharClass = FSharpTokenCharKind.Identifier

  let inline private isOperator t = t.CharClass = FSharpTokenCharKind.Operator

  let inline private isKeyword t = t.ColorClass = FSharpTokenColorKind.Keyword

  let inline private isPunctuation t = t.ColorClass = FSharpTokenColorKind.Punctuation

  let inline private (|GenericTypeParameterPrefix|StaticallyResolvedTypeParameterPrefix|ActivePattern|Other|)
    ((token: FSharpTokenInfo), (lineStr: string))
    =
    if token.Tag = FSharpTokenTag.QUOTE then
      GenericTypeParameterPrefix
    elif token.Tag = FSharpTokenTag.INFIX_AT_HAT_OP then
      // The lexer return INFIX_AT_HAT_OP token for both "^" and "@" symbols.
      // We have to check the char itself to distinguish one from another.
      if token.FullMatchedLength = 1 && lineStr.[token.LeftColumn] = '^' then
        StaticallyResolvedTypeParameterPrefix
      else
        Other
    elif token.Tag = FSharpTokenTag.LPAREN then
      if
        token.FullMatchedLength = 1
        && lineStr.Length > token.LeftColumn + 1
        && lineStr.[token.LeftColumn + 1] = '|'
      then
        ActivePattern
      else
        Other
    else
      Other

  // Operators: Filter out overlapped operators (>>= operator is tokenized as three distinct tokens: GREATER, GREATER, EQUALS.
  // Each of them has FullMatchedLength = 3. So, we take the first GREATER and skip the other two).
  //
  // Generic type parameters: we convert QUOTE + IDENT tokens into single IDENT token, altering its LeftColumn
  // and FullMethodLength (for "'type" which is tokenized as (QUOTE, left=2) + (IDENT, left=3, length=4)
  // we'll get (IDENT, left=2, length=5).
  //
  // Statically resolved type parameters: we convert INFIX_AT_HAT_OP + IDENT tokens into single IDENT token, altering its LeftColumn
  // and FullMethodLength (for "^type" which is tokenized as (INFIX_AT_HAT_OP, left=2) + (IDENT, left=3, length=4)
  // we'll get (IDENT, left=2, length=5).
  let private fixTokens lineStr (tokens: FSharpTokenInfo list) =
    tokens
    |> List.fold
      (fun (acc, (lastToken: DraftToken option)) token ->
        match lastToken with
        //Operator starting with . (like .>>) should be operator
        | Some({ Kind = SymbolKind.Dot } as lastToken) when
          isOperator token && token.LeftColumn <= int lastToken.RightColumn
          ->
          let mergedToken =
            { lastToken.Token with
                Tag = token.Tag
                RightColumn = token.RightColumn }

          acc,
          Some
            { lastToken with
                Token = mergedToken
                Kind = SymbolKind.Operator }
        | Some t when token.LeftColumn <= int t.RightColumn -> acc, lastToken
        | Some({ Kind = SymbolKind.ActivePattern } as lastToken) when
          token.Tag = FSharpTokenTag.BAR
          || token.Tag = FSharpTokenTag.IDENT
          || token.Tag = FSharpTokenTag.UNDERSCORE
          ->
          let mergedToken =
            { lastToken.Token with
                Tag = FSharpTokenTag.IDENT
                RightColumn = token.RightColumn
                FullMatchedLength = lastToken.Token.FullMatchedLength + token.FullMatchedLength }

          acc,
          Some
            { lastToken with
                Token = mergedToken
                RightColumn = lastToken.RightColumn + uint32 token.FullMatchedLength }
        | _ ->
          match token, lineStr with
          | GenericTypeParameterPrefix -> acc, Some(DraftToken.Create GenericTypeParameter token)
          | StaticallyResolvedTypeParameterPrefix -> acc, Some(DraftToken.Create StaticallyResolvedTypeParameter token)
          | ActivePattern -> acc, Some(DraftToken.Create ActivePattern token)
          | Other ->
            let draftToken =
              match lastToken with
              | Some { Kind = GenericTypeParameter | StaticallyResolvedTypeParameter as kind } when isIdentifier token ->
                DraftToken.Create
                  kind
                  { token with
                      LeftColumn = token.LeftColumn - 1
                      FullMatchedLength = token.FullMatchedLength + 1 }
              | Some({ Kind = SymbolKind.ActivePattern } as ap) when token.Tag = FSharpTokenTag.RPAREN ->
                DraftToken.Create SymbolKind.Ident ap.Token
              | Some({ Kind = SymbolKind.Operator } as op) when token.Tag = FSharpTokenTag.RPAREN ->
                DraftToken.Create SymbolKind.Operator op.Token
              // ^ operator
              | Some { Kind = SymbolKind.StaticallyResolvedTypeParameter } ->
                { Kind = SymbolKind.Operator
                  RightColumn = uint32 (token.RightColumn - 1)
                  Token = token }
              | _ ->
                let kind =
                  if isOperator token then Operator
                  elif isIdentifier token then Ident
                  elif isKeyword token then Keyword
                  elif isPunctuation token then Dot
                  else Other

                DraftToken.Create kind token

            draftToken :: acc, Some draftToken)
      ([], None)
    |> fst

  // Returns symbol at a given position.
  let private getSymbolFromTokens
    (tokens: FSharpTokenInfo list)
    (line: uint32)
    (col: uint32)
    (lineStr: string)
    lookupKind
    : LexerSymbol option =
    let tokens = fixTokens lineStr tokens

    // One or two tokens that in touch with the cursor (for "let x|(g) = ()" the tokens will be "x" and "(")
    let tokensUnderCursor =
      match lookupKind with
      | SymbolLookupKind.Simple
      | SymbolLookupKind.Fuzzy ->
        tokens
        |> List.filter (fun x -> x.Token.LeftColumn <= int col && x.RightColumn + 1u >= col)
      | SymbolLookupKind.ForCompletion ->
        tokens
        |> List.filter (fun x -> x.Token.LeftColumn <= int col && x.RightColumn >= col)
      | SymbolLookupKind.ByLongIdent -> tokens |> List.filter (fun x -> x.Token.LeftColumn <= int col)

    match lookupKind with
    | SymbolLookupKind.ByLongIdent ->
      // Try to find start column of the long identifiers
      // Assume that tokens are ordered in an decreasing order of start columns
      let rec tryFindStartColumn tokens =
        match tokens with
        | { Kind = Ident; Token = t1 } :: { Kind = SymbolKind.Other | SymbolKind.Dot
                                            Token = t2 } :: remainingTokens ->
          if t2.Tag = FSharpTokenTag.DOT then
            tryFindStartColumn remainingTokens
          else
            Some(uint32 t1.LeftColumn)
        | { Kind = Ident; Token = t } :: _ -> Some(uint32 t.LeftColumn)
        | _ :: _
        | [] -> None

      let decreasingTokens =
        match tokensUnderCursor |> List.sortBy (fun token -> -token.Token.LeftColumn) with
        // Skip the first dot if it is the start of the identifier
        | { Kind = SymbolKind.Other | SymbolKind.Dot
            Token = t } :: remainingTokens when t.Tag = FSharpTokenTag.DOT -> remainingTokens
        | newTokens -> newTokens

      match decreasingTokens with
      | [] -> None
      | first :: _ ->
        tryFindStartColumn decreasingTokens
        |> Option.map (fun leftCol ->
          { Kind = Ident
            Line = line
            LeftColumn = leftCol
            RightColumn = first.RightColumn + 1u
            Text = lineStr.[int leftCol .. int first.RightColumn] })
    | SymbolLookupKind.Fuzzy ->
      // Select IDENT token. If failed, select OPERATOR token.
      tokensUnderCursor
      |> List.tryFind (fun { DraftToken.Kind = k } ->
        match k with
        | Ident
        | GenericTypeParameter
        | StaticallyResolvedTypeParameter
        | Keyword -> true
        | _ -> false)
      // Gets the option if Some x, otherwise try to get another value

      |> Option.orElseWith (fun _ -> tokensUnderCursor |> List.tryFind (fun { DraftToken.Kind = k } -> k = Operator))
      |> Option.map (fun token ->
        { Kind = token.Kind
          Line = line
          LeftColumn = uint32 token.Token.LeftColumn
          RightColumn = token.RightColumn + 1u
          Text = lineStr.Substring(token.Token.LeftColumn, token.Token.FullMatchedLength) })
    | SymbolLookupKind.ForCompletion
    | SymbolLookupKind.Simple ->
      tokensUnderCursor
      |> List.tryLast
      |> Option.map (fun token ->
        { Kind = token.Kind
          Line = line
          LeftColumn = uint32 token.Token.LeftColumn
          RightColumn = token.RightColumn + 1u
          Text = lineStr.Substring(token.Token.LeftColumn, token.Token.FullMatchedLength) })

  let getSymbol (line: uint32) (col: uint32) lineStr lookupKind (args: string[]) =
    let tokens = tokenizeLine args lineStr

    try
      getSymbolFromTokens tokens line col lineStr lookupKind
    with e ->
      //LoggingService.LogInfo (sprintf "Getting lex symbols failed with %O" e)
      None

  let inline private tryGetLexerSymbolIslands sym =
    match sym.Text with
    | "" -> None
    | _ -> Some(sym.RightColumn, sym.Text.Split '.')

  // Parsing - find the identifier around the current location
  // (we look for full identifier in the backward direction, but only
  // for a short identifier forward - this means that when you hover
  // 'B' in 'A.B.C', you will get intellisense for 'A.B' module)
  let findIdents (col: uint32) lineStr lookupType =
    if lineStr = "" then
      None
    else
      getSymbol 0u col lineStr lookupType [||] |> Option.bind tryGetLexerSymbolIslands

  let findLongIdents (col, lineStr) = findIdents col lineStr SymbolLookupKind.Fuzzy

  let findLongIdentsAndResidue (col: uint32, lineStr: string) =
    let lineStr = lineStr.Substring(0, int col)

    match getSymbol 0u col lineStr SymbolLookupKind.ByLongIdent [||] with
    | Some sym ->
      match sym.Text with
      | "" -> [], ""
      | text ->
        let res = text.Split '.' |> List.ofArray |> List.rev

        if lineStr.[int col - 1] = '.' then
          res |> List.rev, ""
        else
          match res with
          | head :: tail -> tail |> List.rev, head
          | [] -> [], ""
    | _ -> [], ""

  let findClosestIdent col lineStr =
    let tokens = tokenizeLine [||] lineStr
    let fixedTokens = fixTokens lineStr tokens

    let tokensBeforeCol =
      fixedTokens // these are reversed already
      |> List.filter (fun tok -> tok.RightColumn < col)

    tokensBeforeCol
    |> List.tryFind (fun tok -> tok.Kind = Ident)
    |> Option.bind (fun tok -> findIdents tok.RightColumn lineStr SymbolLookupKind.ByLongIdent)
