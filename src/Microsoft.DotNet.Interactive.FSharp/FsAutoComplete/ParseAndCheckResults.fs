namespace FsAutoComplete

open FsAutoComplete.UntypedAstUtils
open FSharp.Compiler
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Text
open FSharp.Compiler.Symbols
open FSharp.Compiler.CodeAnalysis
open System
open System.IO
open Utils
open FSharp.Compiler.Tokenization
open FSharp.Compiler.Syntax

[<RequireQualifiedAccess>]
module TryGetToolTipEnhancedResult =

  type SymbolInfo =
    | Keyword of string
    | Symbol of
      {| XmlDocSig: string
         Assembly: string |}

type TryGetToolTipEnhancedResult =
  { ToolTipText: ToolTipText
    Signature: string
    Footer: string
    SymbolInfo: TryGetToolTipEnhancedResult.SymbolInfo }

type ParseAndCheckResults
  (parseResults: FSharpParseFileResults, checkResults: FSharpCheckFileResults, entityCache: EntityCache) =

  member __.TryGetToolTip (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
    | None ->
      Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"
    | Some(col, identIsland) ->
      let identIsland = Array.toList identIsland
      // TODO: Display other tooltip types, for example for strings or comments where appropriate
      let tip =
        checkResults.GetToolTip(pos.Line, int col, lineStr, identIsland, FSharpTokenTag.Identifier)

      match tip with
      | ToolTipText(elems) when elems |> List.forall ((=) ToolTipElement.None) ->
        match identIsland with
        | [ ident ] ->
          match KeywordList.keywordTooltips.TryGetValue ident with
          | true, tip -> Ok tip
          | _ ->
            Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"
        | _ ->
          Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"
      | _ -> Ok tip

  member x.TryGetToolTipEnhanced (pos: Position) (lineStr: LineStr) : Result<option<TryGetToolTipEnhancedResult>, string> =
    let (|EmptyTooltip|_|) (ToolTipText elems) =
      match elems with
      | [] -> Some()
      | elems when elems |> List.forall ((=) ToolTipElement.None) -> Some()
      | _ -> None

    match Completion.atPos (pos, x.GetParseResults.ParseTree) with
    | Completion.Context.StringLiteral -> Ok None
    | Completion.Context.SynType
    | Completion.Context.Unknown ->
      match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
      | None ->
        Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"
      | Some(col, identIsland) ->
        let identIsland = Array.toList identIsland
        // TODO: Display other tooltip types, for example for strings or comments where appropriate
        let tip =
          checkResults.GetToolTip(pos.Line, int col, lineStr, identIsland, FSharpTokenTag.Identifier)

        let symbol =
          checkResults.GetSymbolUseAtLocation(pos.Line, int col, lineStr, identIsland)

        match tip with
        | EmptyTooltip when symbol.IsNone ->
          match identIsland with
          | [ ident ] ->
            match KeywordList.keywordTooltips.TryGetValue ident with
            | true, tip ->
              { ToolTipText = tip
                Signature = ident
                Footer = ""
                SymbolInfo = TryGetToolTipEnhancedResult.Keyword ident }
              |> Some |> Ok
            | _ ->
              Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"

          | _ ->
            Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"
        | _ ->
          match symbol with
          | None ->
            Error $"Cannot find ident for tooltip: {pos.Column:column} in {lineStr:lineString}"
          | Some symbol ->

            // Retrieve the FSharpSymbol instance so we can find the XmlDocSig
            // This mimic, the behavior of the Info Panel on hover
            // 1. If this is a concrete type it returns that type reference
            // 2. If this a type alias, it returns the aliases type reference
            let resolvedType = symbol.Symbol.GetAbbreviatedParent()

            match SignatureFormatter.getTooltipDetailsFromSymbolUse symbol with
            | None ->
              Error $"Cannot find tooltip for {symbol:symbol} ({pos.Column:column} in {lineStr:lineString})"

            | Some(signature, footer) ->
              { ToolTipText = tip
                Signature = signature
                Footer = footer
                SymbolInfo =
                  TryGetToolTipEnhancedResult.Symbol
                    {| XmlDocSig = resolvedType.XmlDocSig
                       Assembly = symbol.Symbol.Assembly.SimpleName |} }
              |> Some |> Ok

  member __.TryGetFormattedDocumentation (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
    | None -> Error "Cannot find ident"
    | Some(col, identIsland) ->
      let identIsland = Array.toList identIsland
      // TODO: Display other tooltip types, for example for strings or comments where appropriate
      let tip =
        checkResults.GetToolTip(pos.Line, int col, lineStr, identIsland, FSharpTokenTag.Identifier)

      let symbol =
        checkResults.GetSymbolUseAtLocation(pos.Line, int col, lineStr, identIsland)

      match tip with
      | ToolTipText(elems) when elems |> List.forall ((=) ToolTipElement.None) && symbol.IsNone ->
        match identIsland with
        | [ ident ] ->
          match KeywordList.keywordTooltips.TryGetValue ident with
          | true, tip -> Ok(Some tip, None, (ident, DocumentationFormatter.EntityInfo.Empty), "", "")
          | _ -> Error "No tooltip information"
        | _ -> Error "No documentation information"
      | _ ->
        match symbol with
        | None -> Error "No documentation information"
        | Some symbol ->
          match DocumentationFormatter.getTooltipDetailsFromSymbolUse symbol with
          | None -> Error "No documentation information"
          | Some(signature, footer, cn) ->
            match symbol with
            | SymbolUse.TypeAbbreviation symbol ->
              Ok(
                None,
                Some(
                  symbol.GetAbbreviatedParent().XmlDocSig,
                  symbol.GetAbbreviatedParent().Assembly.FileName |> Option.defaultValue ""
                ),
                signature,
                footer,
                cn
              )
            | _ -> Ok(Some tip, None, signature, footer, cn)

  member x.TryGetFormattedDocumentationForSymbol (xmlSig: string) (assembly: string) =
    let entities = x.GetAllEntities false

    let ent =
      entities
      |> List.tryFind (fun e ->
        let check = (e.Symbol.XmlDocSig = xmlSig && e.Symbol.Assembly.SimpleName = assembly)

        if not check then
          match e.Symbol with
          | FSharpEntity(_, abrvEnt, _) -> abrvEnt.XmlDocSig = xmlSig && abrvEnt.Assembly.SimpleName = assembly
          | _ -> false
        else
          true)

    let ent =
      match ent with
      | Some ent -> Some ent
      | None ->
        entities
        |> List.tryFind (fun e ->
          let check = (e.Symbol.XmlDocSig = xmlSig)

          if not check then
            match e.Symbol with
            | FSharpEntity(_, abrvEnt, _) -> abrvEnt.XmlDocSig = xmlSig
            | _ -> false
          else
            true)

    let symbol =
      match ent with
      | Some ent -> Some ent.Symbol
      | None ->
        entities
        |> List.tryPick (fun e ->
          match e.Symbol with
          | FSharpEntity(ent, _, _) ->
            match ent.MembersFunctionsAndValues |> Seq.tryFind (fun f -> f.XmlDocSig = xmlSig) with
            | Some e -> Some(e :> FSharpSymbol)
            | None ->
              match ent.FSharpFields |> Seq.tryFind (fun f -> f.XmlDocSig = xmlSig) with
              | Some e -> Some(e :> FSharpSymbol)
              | None -> None
          | _ -> None)

    match symbol with
    | None -> Error "No matching symbol information"
    | Some symbol ->
      match DocumentationFormatter.getTooltipDetailsFromSymbol symbol with
      | None -> Error "No tooltip information"
      | Some(signature, footer, cn) ->
        Ok(symbol.XmlDocSig, symbol.Assembly.FileName |> Option.defaultValue "", symbol.XmlDoc, signature, footer, cn)

  member __.TryGetSymbolUse (pos: Position) (lineStr: LineStr) : FSharpSymbolUse option =
    match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
    | None -> None
    | Some(colu, identIsland) ->
      let identIsland = Array.toList identIsland
      checkResults.GetSymbolUseAtLocation(pos.Line, int colu, lineStr, identIsland)

  member x.TryGetSymbolUseFromIdent (sourceText: ISourceText) (ident: Ident) : FSharpSymbolUse option =
    let line = sourceText.GetLineString(ident.idRange.EndLine - 1)
    x.GetCheckResults.GetSymbolUseAtLocation(ident.idRange.EndLine, ident.idRange.EndColumn, line, [ ident.idText ])

  member __.TryGetSymbolUses (pos: Position) (lineStr: LineStr) : FSharpSymbolUse list =
    match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
    | None -> []
    | Some(colu, identIsland) ->
      let identIsland = Array.toList identIsland
      checkResults.GetSymbolUsesAtLocation(pos.Line, int colu, lineStr, identIsland)

  member x.TryGetSymbolUseAndUsages (pos: Position) (lineStr: LineStr) =
    let symbolUse = x.TryGetSymbolUse pos lineStr

    match symbolUse with
    | None -> ResultOrString.Error "No symbol information found"
    | Some symbolUse ->
      let symbolUses = checkResults.GetUsesOfSymbolInFile symbolUse.Symbol
      Ok(symbolUse, symbolUses)

  member __.TryGetSignatureData (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
    | None -> ResultOrString.Error "No ident at this location"
    | Some(colu, identIsland) ->

      let identIsland = Array.toList identIsland

      let symbolUse =
        checkResults.GetSymbolUseAtLocation(pos.Line, int colu, lineStr, identIsland)

      match symbolUse with
      | None -> ResultOrString.Error "No symbol information found"
      | Some symbolUse ->
        let fsym = symbolUse.Symbol

        match fsym with
        | :? FSharpMemberOrFunctionOrValue as symbol ->
          let typ =
            symbol.ReturnParameter.Type.Format(symbolUse.DisplayContext.WithPrefixGenericParameters())

          if symbol.IsPropertyGetterMethod then
            Ok(typ, [], [])
          else
            let symbol =
              // Symbol is a property with both get and set.
              // Take the setter symbol in this case.
              if symbol.HasGetterMethod && symbol.HasSetterMethod then
                symbol.SetterMethod
              else
                symbol

            let parms =
              symbol.CurriedParameterGroups
              |> Seq.map (
                Seq.map (fun p -> p.DisplayName, p.Type.Format(symbolUse.DisplayContext.WithPrefixGenericParameters()))
                >> Seq.toList
              )
              |> Seq.toList

            let generics =
              symbol.GenericParameters |> Seq.map (fun generic -> generic.Name) |> Seq.toList
            // Abstract members and abstract member overrides with one () parameter seem have a list with an empty list
            // as parameters.
            match parms with
            | [ [] ] when symbol.IsMember && (not symbol.IsPropertyGetterMethod) ->
              Ok(typ, [ [ ("unit", "unit") ] ], [])
            | _ -> Ok(typ, parms, generics)
        | :? FSharpField as symbol ->
          let typ = symbol.FieldType.Format symbolUse.DisplayContext
          Ok(typ, [], [])
        | _ -> ResultOrString.Error "Not a member, function or value"

  member __.TryGetF1Help (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (uint32 pos.Column, lineStr) with
    | None -> ResultOrString.Error "No ident at this location"
    | Some(colu, identIsland) ->

      let identIsland = Array.toList identIsland
      let help = checkResults.GetF1Keyword(pos.Line, int colu, lineStr, identIsland)

      match help with
      | None -> ResultOrString.Error "No symbol information found"
      | Some hlp -> Ok hlp

  member x.TryGetCompletions (pos: Position) (lineStr: LineStr) (getAllSymbols: unit -> AssemblySymbol list) =
    async {
      let completionContext = Completion.atPos (pos, x.GetParseResults.ParseTree)

      match completionContext with
      | Completion.Context.StringLiteral -> return None
      | Completion.Context.Unknown
      | Completion.Context.SynType ->
        try
          let longName = QuickParse.GetPartialLongNameEx(lineStr, pos.Column - 1)

          let getSymbols () =
            [ for assemblySymbol in getAllSymbols () do
                if
                  assemblySymbol.FullName.Contains(".")
                  && not (PrettyNaming.IsOperatorDisplayName assemblySymbol.Symbol.DisplayName)
                then
                  yield assemblySymbol ]

          let fcsCompletionContext =
            ParsedInput.TryGetCompletionContext(pos, x.GetParseResults.ParseTree, lineStr)

          let results =
            checkResults.GetDeclarationListInfo(
              Some parseResults,
              pos.Line,
              lineStr,
              longName,
              getAllEntities = getSymbols,
              completionContextAtPos = (pos, fcsCompletionContext)
            )

          let getKindPriority kind =
            match kind with
            | CompletionItemKind.SuggestedName
            | CompletionItemKind.CustomOperation -> 0
            | CompletionItemKind.Property -> 1
            | CompletionItemKind.Field -> 2
            | CompletionItemKind.Method(isExtension = false) -> 3
            | CompletionItemKind.Event -> 4
            | CompletionItemKind.Argument -> 5
            | CompletionItemKind.Other -> 6
            | CompletionItemKind.Method(isExtension = true) -> 7

          Array.sortInPlaceWith
            (fun (x: DeclarationListItem) (y: DeclarationListItem) ->
              let mutable n = (not x.IsResolved).CompareTo(not y.IsResolved)

              if n <> 0 then
                n
              else
                n <- (getKindPriority x.Kind).CompareTo(getKindPriority y.Kind)

                if n <> 0 then
                  n
                else
                  n <- (not x.IsOwnMember).CompareTo(not y.IsOwnMember)

                  if n <> 0 then
                    n
                  else
                    n <- String.Compare(x.NameInList, y.NameInList, StringComparison.OrdinalIgnoreCase)

                    if n <> 0 then
                      n
                    else
                      x.MinorPriority.CompareTo(y.MinorPriority))
            results.Items


          let shouldKeywords =
            results.Items.Length > 0
            && not results.IsForType
            && not results.IsError
            && List.isEmpty longName.QualifyingIdents

          return Some(results.Items, longName.PartialIdent, shouldKeywords)
        with :? TimeoutException ->
          return None
    }

  member __.GetAllEntities(publicOnly: bool) : AssemblySymbol list =
    try
      let res =
        [ yield!
            AssemblyContent.GetAssemblySignatureContent AssemblyContentType.Full checkResults.PartialAssemblySignature
          let ctx = checkResults.ProjectContext

          let assembliesByFileName =
            ctx.GetReferencedAssemblies()
            |> List.groupBy (fun asm -> asm.FileName)
            |> List.rev // if mscorlib.dll is the first then FSC raises exception when we try to
          // get Content.Entities from it.

          for fileName, signatures in assembliesByFileName do
            let contentType =
              if publicOnly then
                AssemblyContentType.Public
              else
                AssemblyContentType.Full

            let content =
              AssemblyContent.GetAssemblyContent entityCache.Locking contentType fileName signatures

            yield! content ]

      res
    with _ ->
      []

  member __.GetAllSymbolUsesInFile() = checkResults.GetAllUsesOfAllSymbolsInFile()

  member __.GetSemanticClassification = checkResults.GetSemanticClassification None
  member __.GetAST = parseResults.ParseTree
  member __.GetCheckResults: FSharpCheckFileResults = checkResults
  member __.GetParseResults: FSharpParseFileResults = parseResults
  member __.FileName: string = Utils.normalizePath parseResults.FileName
