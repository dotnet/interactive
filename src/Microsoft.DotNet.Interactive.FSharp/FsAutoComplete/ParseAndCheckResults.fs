namespace FsAutoComplete

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

type ParseAndCheckResults
  (
    parseResults: FSharpParseFileResults,
    checkResults: FSharpCheckFileResults,
    entityCache: EntityCache
  ) =

  member __.TryGetToolTip (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (pos.Column, lineStr) with
    | None -> ResultOrString.Error "Cannot find ident for tooltip"
    | Some (col, identIsland) ->
      let identIsland = Array.toList identIsland
      // TODO: Display other tooltip types, for example for strings or comments where appropriate
      let tip =
        checkResults.GetToolTip(pos.Line, col, lineStr, identIsland, FSharpTokenTag.Identifier)

      match tip with
      | ToolTipText (elems) when elems |> List.forall ((=) ToolTipElement.None) ->
        match identIsland with
        | [ ident ] ->
          match KeywordList.keywordTooltips.TryGetValue ident with
          | true, tip -> Ok tip
          | _ -> ResultOrString.Error "No tooltip information"
        | _ -> ResultOrString.Error "No tooltip information"
      | _ -> Ok(tip)

  member x.TryGetToolTipEnhanced (pos: Position) (lineStr: LineStr) =
    let (|EmptyTooltip|_|) (ToolTipText elems) =
        match elems with
        | [] -> Some()
        | elems when elems |> List.forall ((=) ToolTipElement.None) -> Some()
        | _ -> None

    match Lexer.findLongIdents (pos.Column, lineStr) with
    | None -> Error "Cannot find ident for tooltip"
    | Some (col, identIsland) ->
    let identIsland = Array.toList identIsland
    // TODO: Display other tooltip types, for example for strings or comments where appropriate
    let tip =
        checkResults.GetToolTip(pos.Line, col, lineStr, identIsland, FSharpTokenTag.Identifier)

    let symbol =
        checkResults.GetSymbolUseAtLocation(pos.Line, col, lineStr, identIsland)

    match tip with
    | EmptyTooltip when symbol.IsNone ->
        match identIsland with
        | [ ident ] ->
            match KeywordList.keywordTooltips.TryGetValue ident with
            | true, tip -> Ok(Some(tip, ident, "", None))
            | _ -> Error "No tooltip information"
        | _ -> Error "No tooltip information"
    | _ ->
        match symbol with
        | None -> Error "No tooltip information"
        | Some symbol ->

        match SignatureFormatter.getTooltipDetailsFromSymbolUse symbol with
        | None -> Error "No tooltip information"
        | Some (signature, footer) ->
            let typeDoc =
                getTypeIfConstructor symbol.Symbol |> Option.map (fun n -> n.XmlDocSig)

            Ok(Some(tip, signature, footer, typeDoc))

  member __.TryGetFormattedDocumentation (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (pos.Column, lineStr) with
    | None -> Error "Cannot find ident"
    | Some (col, identIsland) ->
      let identIsland = Array.toList identIsland
      // TODO: Display other tooltip types, for example for strings or comments where appropriate
      let tip =
        checkResults.GetToolTip(pos.Line, col, lineStr, identIsland, FSharpTokenTag.Identifier)

      let symbol =
        checkResults.GetSymbolUseAtLocation(pos.Line, col, lineStr, identIsland)

      match tip with
      | ToolTipText (elems) when elems |> List.forall ((=) ToolTipElement.None) && symbol.IsNone ->
        match identIsland with
        | [ ident ] ->
          match KeywordList.keywordTooltips.TryGetValue ident with
          | true, tip -> Ok(Some tip, None, (ident, (DocumentationFormatter.emptyTypeTip)), "", "")
          | _ -> Error "No tooltip information"
        | _ -> Error "No documentation information"
      | _ ->
        match symbol with
        | None -> Error "No documentation information"
        | Some symbol ->
          match DocumentationFormatter.getTooltipDetailsFromSymbolUse symbol with
          | None -> Error "No documentation information"
          | Some (signature, footer, cn) ->
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
          | FSharpEntity (_, abrvEnt, _) -> abrvEnt.XmlDocSig = xmlSig && abrvEnt.Assembly.SimpleName = assembly
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
            | FSharpEntity (_, abrvEnt, _) -> abrvEnt.XmlDocSig = xmlSig
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
          | FSharpEntity (ent, _, _) ->
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
      | Some (signature, footer, cn) ->
        Ok(symbol.XmlDocSig, symbol.Assembly.FileName |> Option.defaultValue "", symbol.XmlDoc, signature, footer, cn)

  member __.TryGetSymbolUse (pos: Position) (lineStr: LineStr) : FSharpSymbolUse option =
    match Lexer.findLongIdents (pos.Column, lineStr) with
    | None -> None
    | Some (colu, identIsland) ->
      let identIsland = Array.toList identIsland
      checkResults.GetSymbolUseAtLocation(pos.Line, colu, lineStr, identIsland)

  member x.TryGetSymbolUseAndUsages (pos: Position) (lineStr: LineStr) =
    let symboluse = x.TryGetSymbolUse pos lineStr

    match symboluse with
    | None -> ResultOrString.Error "No symbol information found"
    | Some symboluse ->
      let symboluses = checkResults.GetUsesOfSymbolInFile symboluse.Symbol
      Ok(symboluse, symboluses)

  member __.TryGetSignatureData (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (pos.Column, lineStr) with
    | None -> ResultOrString.Error "No ident at this location"
    | Some (colu, identIsland) ->

      let identIsland = Array.toList identIsland

      let symboluse =
        checkResults.GetSymbolUseAtLocation(pos.Line, colu, lineStr, identIsland)

      match symboluse with
      | None -> ResultOrString.Error "No symbol information found"
      | Some symboluse ->
        let fsym = symboluse.Symbol

        match fsym with
        | :? FSharpMemberOrFunctionOrValue as symbol ->
          let typ =
            symbol.ReturnParameter.Type.Format(symboluse.DisplayContext.WithPrefixGenericParameters())

          if symbol.IsPropertyGetterMethod then
            Ok(typ, [], [])
          else
            let parms =
              symbol.CurriedParameterGroups
              |> Seq.map (
                Seq.map (fun p -> p.DisplayName, p.Type.Format(symboluse.DisplayContext.WithPrefixGenericParameters()))
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
          let typ = symbol.FieldType.Format symboluse.DisplayContext
          Ok(typ, [], [])
        | _ -> ResultOrString.Error "Not a member, function or value"

  member __.TryGetF1Help (pos: Position) (lineStr: LineStr) =
    match Lexer.findLongIdents (pos.Column, lineStr) with
    | None -> ResultOrString.Error "No ident at this location"
    | Some (colu, identIsland) ->

      let identIsland = Array.toList identIsland
      let help = checkResults.GetF1Keyword(pos.Line, colu, lineStr, identIsland)

      match help with
      | None -> ResultOrString.Error "No symbol information found"
      | Some hlp -> Ok hlp

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

  member __.GetAllSymbolUsesInFile() =
    checkResults.GetAllUsesOfAllSymbolsInFile()

  member __.GetSemanticClassification = checkResults.GetSemanticClassification None
  member __.GetAST = parseResults.ParseTree
  member __.GetCheckResults: FSharpCheckFileResults = checkResults
  member __.GetParseResults: FSharpParseFileResults = parseResults
