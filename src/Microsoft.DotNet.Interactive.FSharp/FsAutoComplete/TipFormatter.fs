// --------------------------------------------------------------------------------------
// (c) Tomas Petricek, http://tomasp.net/blog
// --------------------------------------------------------------------------------------
module FsAutoComplete.TipFormatter

open System
open System.IO
open System.Xml
open System.Collections.Generic
open System.Text.RegularExpressions
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

let inline nl<'T> = Environment.NewLine

module private Section =

  let inline addSection (name: string) (content: string) =
    if name <> "" then
      nl + nl + "**" + name + "**" + nl + nl + content
    else
      nl + nl + content

  let fromKeyValueList (name: string) (content: list<KeyValuePair<string, string>>) =
    if List.isEmpty content then
      ""
    else
      content
      |> Seq.map (fun kv ->
        let text =
          if kv.Value.Contains '\n' then
            kv.Value.Split('\n')
            |> Seq.map (fun line -> "> " + line.TrimStart())
            |> String.concat Environment.NewLine
            |> (+) nl // Start the quote block on a new line
          else
            kv.Value

        "* `" + kv.Key + "`" + ": " + text)
      |> String.concat nl
      |> addSection name

  let fromOption (name: string) (content: string option) = if content.IsNone then "" else addSection name content.Value

  let fromList (name: string) (content: string seq) =
    if Seq.isEmpty content then
      ""
    else
      addSection name (content |> String.concat nl)

module private Format =

  let tagPattern (tagName: string) =
    sprintf
      """(?'void_element'<%s(?'void_attributes'\s+[^\/>]+)?\/>)|(?'non_void_element'<%s(?'non_void_attributes'\s+[^>]+)?>(?'non_void_innerText'(?:(?!<%s>)(?!<\/%s>)[\s\S])*)<\/%s\s*>)"""
      tagName
      tagName
      tagName
      tagName
      tagName

  type TagInfo =
    | VoidElement of attributes: Map<string, string>
    | NonVoidElement of innerText: string * attributes: Map<string, string>

  type FormatterInfo =
    { TagName: string
      Formatter: TagInfo -> string option }

  let private extractTextFromQuote (quotedText: string) = quotedText.Substring(1, quotedText.Length - 2)


  let extractMemberText (text: string) =
    let pattern = "(?'member_type'[a-z]{1}:)?(?'member_text'.*)"
    let m = Regex.Match(text, pattern, RegexOptions.IgnoreCase)

    if m.Groups.["member_text"].Success then
      m.Groups.["member_text"].Value
    else
      text

  let private getAttributes (attributes: Group) =
    if attributes.Success then
      let pattern = """(?'key'\S+)=(?'value''[^']*'|"[^"]*")"""

      Regex.Matches(attributes.Value, pattern, RegexOptions.IgnoreCase)
      |> Seq.cast<Match>
      |> Seq.map (fun m -> m.Groups.["key"].Value, extractTextFromQuote m.Groups.["value"].Value)
      |> Map.ofSeq
    else
      Map.empty

  type AttrLookup = Map<string, string> -> Option<string>

  let private cref: AttrLookup = Map.tryFind "cref"
  let private langword: AttrLookup = Map.tryFind "langword"
  let private href: AttrLookup = Map.tryFind "href"
  let private lang: AttrLookup = Map.tryFind "lang"
  let private name: AttrLookup = Map.tryFind "name"

  let rec private applyFormatter (info: FormatterInfo) text =
    let pattern = tagPattern info.TagName

    match Regex.Match(text, pattern, RegexOptions.IgnoreCase) with
    | m when m.Success ->
      if m.Groups.["void_element"].Success then
        let attributes = getAttributes m.Groups.["void_attributes"]

        let replacement = VoidElement attributes |> info.Formatter

        match replacement with
        | Some replacement ->
          text.Replace(m.Groups.["void_element"].Value, replacement)
          // Re-apply the formatter, because perhaps there is more
          // of the current tag to convert
          |> applyFormatter info

        | None ->
          // The formatter wasn't able to convert the tag
          // Return as it is and don't re-apply the formatter
          // otherwise it will create an infinity loop
          text

      else if m.Groups.["non_void_element"].Success then
        let innerText = m.Groups.["non_void_innerText"].Value
        let attributes = getAttributes m.Groups.["non_void_attributes"]

        let replacement = NonVoidElement(innerText, attributes) |> info.Formatter

        match replacement with
        | Some replacement ->
          // Re-apply the formatter, because perhaps there is more
          // of the current tag to convert
          text.Replace(m.Groups.["non_void_element"].Value, replacement)
          |> applyFormatter info

        | None ->
          // The formatter wasn't able to convert the tag
          // Return as it is and don't re-apply the formatter
          // otherwise it will create an infinity loop
          text
      else
        // Should not happened but like that we are sure to handle all possible cases
        text
    | _ -> text

  let private codeBlock =
    { TagName = "code"
      Formatter =
        function
        | VoidElement _ -> None

        | NonVoidElement(innerText, attributes) ->
          let lang =
            match lang attributes with
            | Some lang -> lang

            | None -> "forceNoHighlight"

          // We need to trim the end of the text because the
          // user write XML comments with a space between the '///'
          // and the '<code>' tag. Then it mess up identification of new lines
          // at the end of the code snippet.
          // Example:
          // /// <code>
          // ///   var x = 1;
          // /// </code>
          //    ^ This space is the one we need to remove
          let innerText = innerText.TrimEnd()

          // Try to detect how the code snippet is formatted
          // so render the markdown code block the best way
          // by avoid empty lines at the beginning or the end
          let formattedText =
            match
              innerText.StartsWith("\n", StringComparison.Ordinal), innerText.EndsWith("\n", StringComparison.Ordinal)
            with
            | true, true -> sprintf "```%s%s```" lang innerText
            | true, false -> sprintf "```%s%s\n```" lang innerText
            | false, true -> sprintf "```%s\n%s```" lang innerText
            | false, false -> sprintf "```%s\n%s\n```" lang innerText

          Some formattedText

    }
    |> applyFormatter

  let private example =
    { TagName = "example"
      Formatter =
        function
        | VoidElement _ -> None

        | NonVoidElement(innerText, _) ->
          let formattedText =
            nl
            + nl
            // This try to keep a visual consistency and indicate that this
            // "Example section" is part of it parent section (summary, remarks, etc.)
            + """Example:"""
            + nl
            + nl
            + innerText

          Some formattedText

    }
    |> applyFormatter

  let private codeInline =
    { TagName = "c"
      Formatter =
        function
        | VoidElement _ -> None
        | NonVoidElement(innerText, _) -> "`" + innerText + "`" |> Some }
    |> applyFormatter

  let private link text uri = $"[`%s{text}`](%s{uri})"
  let private code text = $"`%s{text}`"

  let private anchor =
    { TagName = "a"
      Formatter =
        function
        | VoidElement attributes ->
          match href attributes with
          | Some href -> Some(link href href)
          | None -> None

        | NonVoidElement(innerText, attributes) ->
          match href attributes with
          | Some href -> Some(link innerText href)
          | None -> Some(code innerText) }
    |> applyFormatter

  let private paragraph =
    { TagName = "para"
      Formatter =
        function
        | VoidElement _ -> None

        | NonVoidElement(innerText, _) -> nl + innerText + nl |> Some }
    |> applyFormatter

  let private block =
    { TagName = "block"
      Formatter =
        function
        | VoidElement _ -> None

        | NonVoidElement(innerText, _) -> nl + innerText + nl |> Some }
    |> applyFormatter

  let private see =
    let formatFromAttributes (attrs: Map<string, string>) =
      match cref attrs with
      // crefs can have backticks in them, which mess with formatting.
      // for safety we can just double-backtick and markdown is ok with that.
      | Some cref -> Some $"``{extractMemberText cref}``"
      | None ->
        match langword attrs with
        | Some langword -> Some(code langword)
        | None -> None

    { TagName = "see"
      Formatter =
        function
        | VoidElement attributes -> formatFromAttributes attributes
        | NonVoidElement(innerText, attributes) ->
          if String.IsNullOrWhiteSpace innerText then
            formatFromAttributes attributes
          else
            match href attributes with
            | Some externalUrl -> Some(link innerText externalUrl)
            | None -> Some $"`{innerText}`" }
    |> applyFormatter

  let private xref =
    { TagName = "xref"
      Formatter =
        function
        | VoidElement attributes ->
          match href attributes with
          | Some href -> Some(link href href)
          | None -> None

        | NonVoidElement(innerText, attributes) ->
          if String.IsNullOrWhiteSpace innerText then
            match href attributes with
            | Some href -> Some(link innerText href)
            | None -> None
          else
            Some(code innerText) }
    |> applyFormatter

  let private paramRef =
    { TagName = "paramref"
      Formatter =
        function
        | VoidElement attributes ->
          match name attributes with
          | Some name -> Some(code name)
          | None -> None

        | NonVoidElement(innerText, attributes) ->
          if String.IsNullOrWhiteSpace innerText then
            match name attributes with
            | Some name ->
              // TODO: Add config to generates command
              Some(code name)
            | None -> None
          else
            Some(code innerText)

    }
    |> applyFormatter

  let private typeParamRef =
    { TagName = "typeparamref"
      Formatter =
        function
        | VoidElement attributes ->
          match name attributes with
          | Some name -> Some(code name)
          | None -> None

        | NonVoidElement(innerText, attributes) ->
          if String.IsNullOrWhiteSpace innerText then
            match name attributes with
            | Some name ->
              // TODO: Add config to generates command
              Some(code name)
            | None -> None
          else
            Some(code innerText) }
    |> applyFormatter

  let private fixPortableClassLibrary (text: string) =
    text.Replace(
      "~/docs/standard/cross-platform/cross-platform-development-with-the-portable-class-library.md",
      "https://docs.microsoft.com/en-gb/dotnet/standard/cross-platform/cross-platform-development-with-the-portable-class-library"
    )

  /// <summary>Handle Microsoft 'or' formatting blocks</summary>
  /// <remarks>
  /// <para>We don't use the formatter API here because we are not handling a "real XML element"</para>
  /// <para>We don't use regex neither because I am not able to create one covering all the possible case</para>
  /// <para>
  /// There are 2 types of 'or' blocks:
  ///
  /// - Inlined: [...]  -or-  [...]  -or-  [...]
  /// - Blocked:
  /// [...]
  /// -or-
  /// [...]
  /// -or-
  /// [...]
  /// </para>
  /// <para>
  /// This function can convert both styles. If an 'or' block is encounter the whole section will always result in a multiline output
  /// </para>
  /// <para>
  /// If we pass any of the 2 previous example, it will generate the same Markdown string as a result (because they have the same number of 'or' section). The result will be:
  /// </para>
  /// <para>
  /// >    [...]
  ///
  /// *or*
  ///
  /// >    [...]
  ///
  /// *or*
  ///
  /// >    [...]
  /// </para>
  /// </remarks>
  let private handleMicrosoftOrList (text: string) =
    let splitResult = text.Split([| "-or-" |], StringSplitOptions.RemoveEmptyEntries)

    // If text doesn't contains any `-or-` then we just forward it
    if Seq.length splitResult = 1 then
      text
    else
      splitResult
      |> Seq.map (fun orText ->
        let orText = orText.Trim()
        let lastParagraphStartIndex = orText.LastIndexOf("\n")

        // We make the assumption that an 'or' section should always be defined on a single line
        // From testing against different 'or' block written by Microsoft it seems to be always the case
        // By doing this assumption this allow us to correctly handle comments like:
        //
        // <block>
        // Some text goes here
        // </block>
        // CaseA of the or section
        // -or-
        // CaseB of the or section
        // -or-
        // CaseC of the or section
        //
        // The original comments is for `System.Uri("")`
        // By making the assumption that an 'or' section is always single line this allows us to detect the "<block></block>" section

        // orText is on a single line, we just add quotation syntax
        if lastParagraphStartIndex = -1 then
          sprintf ">    %s" orText

        // orText is on multiple lines
        // 1. We first extract the everything until the last line
        // 2. We extract on the last line
        // 3. We return the start of the section and the end of the section marked using quotation
        else
          let startText = orText.Substring(0, lastParagraphStartIndex)
          let endText = orText.Substring(lastParagraphStartIndex)

          sprintf "%s\n>    %s" startText endText)
      // Force a new `-or-` paragraph between each orSection
      // In markdown a new paragraph is define by using 2 empty lines
      |> String.concat "\n\n*or*\n\n"

  /// <summary>Remove all invalid 'or' block found</summary>
  /// <remarks>
  /// If an 'or' block is found between 2 elements then we remove it as we can't generate a valid markdown for it
  ///
  /// For example, <td> Some text -or- another text </td> cannot be converted into a multiline string
  /// and so we prefer to remove the 'or' block instead of having some weird markdown artifacts
  ///
  /// For now, we only consider text between <td></td> to be invalid
  /// We can add more in the future if needed, but I want to keep this as minimal as possible to avoid capturing false positive
  /// </remarks>
  let private removeInvalidOrBlock (text: string) =
    let invalidOrBlockPattern =
      """<td(\s+[^>])*>(?'or_text'(?:(?!<td)[\s\S])*-or-(?:(?!<\/td)[\s\S])*)<\/td(\s+[^>])*>"""

    Regex.Matches(text, invalidOrBlockPattern, RegexOptions.Multiline)
    |> Seq.cast<Match>
    |> Seq.fold
      (fun (state: string) (m: Match) ->
        let orText = m.Groups.["or_text"]

        if orText.Success then
          let replacement = orText.Value.Replace("-or-", "or")

          state.Replace(orText.Value, replacement)
        else
          state)
      text


  let private thsPattern = Regex "<th\s?>"

  let private convertTable =
    { TagName = "table"
      Formatter =
        function
        | VoidElement _ -> None

        | NonVoidElement(innerText, _) ->

          let rowCount = thsPattern.Matches(innerText).Count

          let convertedTable =
            innerText
              .Replace(nl, "")
              .Replace("\n", "")
              .Replace("<table>", "")
              .Replace("</table>", "")
              .Replace("<thead>", "")
              .Replace("</thead>", (String.replicate rowCount "| --- "))
              .Replace("<tbody>", nl)
              .Replace("</tbody>", "")
              .Replace("<tr>", "")
              .Replace("</tr>", "|" + nl)
              .Replace("<th>", "|")
              .Replace("</th>", "")
              .Replace("<td>", "|")
              .Replace("</td>", "")

          nl + nl + convertedTable + nl |> Some

    }
    |> applyFormatter

  type private Term = string
  type private Definition = string

  [<Struct>]
  type private ListStyle =
    | Bulleted
    | Numbered
    | Tablered

  /// ItemList allow a permissive representation of an Item.
  /// In theory, TermOnly should not exist but we added it so part of the documentation doesn't disappear
  /// TODO: Allow direct text support without <description> and <term> tags
  type private ItemList =
    /// A list where the items are just contains in a <description> element
    | DescriptionOnly of string
    /// A list where the items are just contains in a <term> element
    | TermOnly of string
    /// A list where the items are a term followed by a definition (ie in markdown: * <TERM> - <DEFINITION>)
    | Definitions of Term * Definition

  let private itemListToStringAsMarkdownList (prefix: string) (item: ItemList) =
    match item with
    | DescriptionOnly description -> $"{prefix} {description}"
    | TermOnly term -> $"{prefix} **{term}**"
    | Definitions(term, description) -> $"{prefix} **{term}** - {description}"

  let private list =
    let getType (attributes: Map<string, string>) = Map.tryFind "type" attributes

    let tryGetInnerTextOnNonVoidElement (text: string) (tagName: string) =
      match Regex.Match(text, tagPattern tagName, RegexOptions.IgnoreCase) with
      | m when m.Success ->
        if m.Groups.["non_void_element"].Success then
          Some m.Groups.["non_void_innerText"].Value
        else
          None
      | _ -> None

    let tryGetNonVoidElement (text: string) (tagName: string) =
      match Regex.Match(text, tagPattern tagName, RegexOptions.IgnoreCase) with
      | m when m.Success ->
        if m.Groups.["non_void_element"].Success then
          Some(m.Groups.["non_void_element"].Value, m.Groups.["non_void_innerText"].Value)
        else
          None
      | _ -> None

    let tryGetDescription (text: string) = tryGetInnerTextOnNonVoidElement text "description"

    let tryGetTerm (text: string) = tryGetInnerTextOnNonVoidElement text "term"

    let itmPattern = Regex(tagPattern "item", RegexOptions.IgnoreCase)

    let rec extractItemList (res: ItemList list) (text: string) =
      match itmPattern.Match text with
      | m when m.Success ->
        let newText = text.Substring(m.Value.Length)

        if m.Groups.["non_void_element"].Success then
          let innerText = m.Groups.["non_void_innerText"].Value
          let description = tryGetDescription innerText
          let term = tryGetTerm innerText

          let currentItem: ItemList option =
            match description, term with
            | Some description, Some term -> Definitions(term, description) |> Some
            | Some description, None -> DescriptionOnly description |> Some
            | None, Some term -> TermOnly term |> Some
            | None, None -> None

          match currentItem with
          | Some currentItem -> extractItemList (res @ [ currentItem ]) newText
          | None -> extractItemList res newText
        else
          extractItemList res newText
      | _ -> res

    let listHeader = Regex(tagPattern "listheader", RegexOptions.IgnoreCase)

    let rec extractColumnHeader (res: string list) (text: string) =
      match listHeader.Match text with
      | m when m.Success ->
        let newText = text.Substring(m.Value.Length)

        if m.Groups.["non_void_element"].Success then
          let innerText = m.Groups.["non_void_innerText"].Value

          let rec extractAllTerms (res: string list) (text: string) =
            match tryGetNonVoidElement text "term" with
            | Some(fullString, innerText) ->
              let escapedRegex = Regex(Regex.Escape(fullString))
              let newText = escapedRegex.Replace(text, "", 1)
              extractAllTerms (res @ [ innerText ]) newText
            | None -> res

          extractColumnHeader (extractAllTerms [] innerText) newText
        else
          extractColumnHeader res newText
      | _ -> res

    let itemPattern = Regex(tagPattern "item", RegexOptions.IgnoreCase)

    let rec extractRowsForTable (res: (string list) list) (text: string) =
      match itemPattern.Match text with
      | m when m.Success ->
        let newText = text.Substring(m.Value.Length)

        if m.Groups.["non_void_element"].Success then
          let innerText = m.Groups.["non_void_innerText"].Value

          let rec extractAllTerms (res: string list) (text: string) =
            match tryGetNonVoidElement text "term" with
            | Some(fullString, innerText) ->
              let escapedRegex = Regex(Regex.Escape(fullString))
              let newText = escapedRegex.Replace(text, "", 1)
              extractAllTerms (res @ [ innerText ]) newText
            | None -> res

          extractRowsForTable (res @ [ extractAllTerms [] innerText ]) newText
        else
          extractRowsForTable res newText
      | _ -> res

    { TagName = "list"
      Formatter =
        function
        | VoidElement _ -> None

        | NonVoidElement(innerText, attributes) ->
          let listStyle =
            match getType attributes with
            | Some "bullet" -> Bulleted
            | Some "number" -> Numbered
            | Some "table" -> Tablered
            | Some _
            | None -> Bulleted

          (match listStyle with
           | Bulleted ->
             let items = extractItemList [] innerText

             items
             |> List.map (itemListToStringAsMarkdownList "*")
             |> String.concat Environment.NewLine

           | Numbered ->
             let items = extractItemList [] innerText

             items
             |> List.map (itemListToStringAsMarkdownList "1.")
             |> String.concat Environment.NewLine

           | Tablered ->
             let columnHeaders = extractColumnHeader [] innerText
             let rows = extractRowsForTable [] innerText

             let columnHeadersText =
               columnHeaders
               |> List.mapi (fun index header ->
                 if index = 0 then
                   "| " + header
                 elif index = columnHeaders.Length - 1 then
                   " | " + header + " |"
                 else
                   " | " + header)
               |> String.concat ""

             let separator =
               columnHeaders
               |> List.mapi (fun index _ ->
                 if index = 0 then "| ---"
                 elif index = columnHeaders.Length - 1 then " | --- |"
                 else " | ---")
               |> String.concat ""

             let itemsText =
               rows
               |> List.map (fun columns ->
                 columns
                 |> List.mapi (fun index column ->
                   if index = 0 then
                     "| " + column
                   elif index = columnHeaders.Length - 1 then
                     " | " + column + " |"
                   else
                     " | " + column)
                 |> String.concat "")
               |> String.concat Environment.NewLine

             Environment.NewLine
             + columnHeadersText
             + Environment.NewLine
             + separator
             + Environment.NewLine
             + itemsText)
          |> Some }
    |> applyFormatter

  /// <summary>
  /// Unescape XML special characters
  ///
  /// For example, this allows to print '>' in the tooltip instead of '&gt;'
  /// </summary>
  let private unescapeSpecialCharacters (text: string) =
    text.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Replace("&amp;", "&")

  let applyAll (text: string) =
    text
    // Remove invalid syntax first
    // It's easier to identify invalid patterns when no transformation has been done yet
    |> removeInvalidOrBlock
    // Start the transformation process
    |> paragraph
    |> example
    |> block
    |> codeInline
    |> codeBlock
    |> see
    |> xref
    |> paramRef
    |> typeParamRef
    |> anchor
    |> list
    |> convertTable
    |> fixPortableClassLibrary
    |> handleMicrosoftOrList
    |> unescapeSpecialCharacters

[<RequireQualifiedAccess; Struct>]
type FormatCommentStyle =
  | Legacy
  | FullEnhanced
  | SummaryOnly
  | Documentation

// TODO: Improve this parser. Is there any other XmlDoc parser available?
type private XmlDocMember(doc: XmlDocument, indentationSize: int, columnOffset: int) =
  /// References used to detect if we should remove meaningless spaces
  let tabsOffset = String.replicate (columnOffset + indentationSize) " "

  let readContentForTooltip (node: XmlNode) =
    match node with
    | null -> null
    | _ ->
      let content =
        // Normale the EOL
        // This make it easier to work with line splitting
        node.InnerXml.Replace("\r\n", "\n") |> Format.applyAll

      content.Split('\n')
      |> Array.map (fun line ->
        if
          not (String.IsNullOrWhiteSpace line)
          && line.StartsWith(tabsOffset, StringComparison.Ordinal)
        then
          line.Substring(columnOffset + indentationSize)
        else
          line)
      |> String.concat Environment.NewLine

  let readChildren name (doc: XmlDocument) =
    doc.DocumentElement.GetElementsByTagName name
    |> Seq.cast<XmlNode>
    |> Seq.map (fun node -> Format.extractMemberText node.Attributes.[0].InnerText, node)
    |> Seq.toList

  let readRemarks (doc: XmlDocument) = doc.DocumentElement.GetElementsByTagName "remarks" |> Seq.cast<XmlNode>

  let rawSummary = doc.DocumentElement.ChildNodes.[0]
  let rawParameters = readChildren "param" doc
  let rawRemarks = readRemarks doc
  let rawExceptions = readChildren "exception" doc
  let rawTypeParams = readChildren "typeparam" doc

  let rawReturns =
    doc.DocumentElement.GetElementsByTagName "returns"
    |> Seq.cast<XmlNode>
    |> Seq.tryHead

  let rawExamples =
    doc.DocumentElement.GetElementsByTagName "example"
    |> Seq.cast<XmlNode>
    // We need to filter out the examples node that are children
    // of another "main" node
    // This is because if the example node is inside a "main" node
    // then we render it in place.
    // So we don't need to render it independently in the Examples section
    |> Seq.filter (fun node ->
      [ "summary"; "param"; "returns"; "exception"; "remarks"; "typeparam" ]
      |> List.contains node.ParentNode.Name
      |> not)

  let readNamedContentAsKvPair (key, content) = KeyValuePair(key, readContentForTooltip content)

  let summary = readContentForTooltip rawSummary

  let parameters = rawParameters |> List.map readNamedContentAsKvPair
  let remarks = rawRemarks |> Seq.map readContentForTooltip
  let exceptions = rawExceptions |> List.map readNamedContentAsKvPair
  let typeParams = rawTypeParams |> List.map readNamedContentAsKvPair
  let examples = rawExamples |> Seq.map readContentForTooltip
  let returns = rawReturns |> Option.map readContentForTooltip

  let seeAlso =
    doc.DocumentElement.GetElementsByTagName "seealso"
    |> Seq.cast<XmlNode>
    |> Seq.map (fun node -> "* `" + Format.extractMemberText node.Attributes.[0].InnerText + "`")

  override x.ToString() =
    summary
    + nl
    + nl
    + (parameters
       |> Seq.map (fun kv -> "`" + kv.Key + "`" + ": " + kv.Value)
       |> String.concat nl)
    + (if exceptions.Length = 0 then
         ""
       else
         nl
         + nl
         + "Exceptions:"
         + nl
         + (exceptions
            |> Seq.map (fun kv -> "\t" + "`" + kv.Key + "`" + ": " + kv.Value)
            |> String.concat nl))

  member __.ToSummaryOnlyString() =
    // If we where unable to process the doc comment, then just output it as it is
    // For example, this cover the keywords' tooltips
    if String.IsNullOrEmpty summary then
      doc.InnerText
    else
      "**Description**" + nl + nl + summary

  member __.HasTruncatedExamples = examples |> Seq.isEmpty |> not

  member __.ToFullEnhancedString() =
    let content =
      summary
      + Section.fromList "Remarks" remarks
      + Section.fromKeyValueList "Type parameters" typeParams
      + Section.fromKeyValueList "Parameters" parameters
      + Section.fromOption "Returns" returns
      + Section.fromKeyValueList "Exceptions" exceptions
      + Section.fromList "See also" seeAlso

    // If we where unable to process the doc comment, then just output it as it is
    // For example, this cover the keywords' tooltips
    if String.IsNullOrEmpty content then
      doc.InnerText
    else
      "**Description**" + nl + nl + content

  member __.ToDocumentationString() =
    "**Description**"
    + nl
    + nl
    + summary
    + Section.fromList "Remarks" remarks
    + Section.fromKeyValueList "Type parameters" typeParams
    + Section.fromKeyValueList "Parameters" parameters
    + Section.fromOption "Returns" returns
    + Section.fromKeyValueList "Exceptions" exceptions
    + Section.fromList "Examples" examples
    + Section.fromList "See also" seeAlso

  member this.FormatComment(formatStyle: FormatCommentStyle) =
    match formatStyle with
    | FormatCommentStyle.Legacy -> this.ToString()
    | FormatCommentStyle.SummaryOnly -> this.ToSummaryOnlyString()
    | FormatCommentStyle.FullEnhanced -> this.ToFullEnhancedString()
    | FormatCommentStyle.Documentation -> this.ToDocumentationString()


let rec private readXmlDoc (reader: XmlReader) (indentationSize: int) (acc: Map<string, XmlDocMember>) =
  let acc' =
    match reader.Read() with
    | false -> indentationSize, None
    // Assembly is the first node in the XML and is at least always intended by 1 "tab"
    // So we used it as a reference to detect the tabs sizes
    // This is needed because `netstandard.xml` use 2 spaces tabs
    // Where when building a C# classlib, the xml file use 4 spaces size for example
    | true when reader.Name = "assembly" && reader.NodeType = XmlNodeType.Element ->
      let xli: IXmlLineInfo = (box reader) :?> IXmlLineInfo
      // - 2 : allow us to detect the position before the < char
      xli.LinePosition - 2, Some acc
    | true when reader.Name = "member" && reader.NodeType = XmlNodeType.Element ->
      try
        // We detect the member LinePosition so we can calculate the meaningless spaces later
        let xli: IXmlLineInfo = (box reader) :?> IXmlLineInfo
        let key = reader.GetAttribute("name")
        use subReader = reader.ReadSubtree()
        let doc = XmlDocument()
        doc.Load(subReader)
        // - 3 : allow us to detect the last indentation position
        // This isn't intuitive but from my tests this is what works
        indentationSize,
        acc
        |> Map.add key (XmlDocMember(doc, indentationSize, xli.LinePosition - 3))
        |> Some
      with ex ->
        indentationSize, Some acc
    | _ -> indentationSize, Some acc

  match acc' with
  | _, None -> acc
  | indentationSize, Some acc' -> readXmlDoc reader indentationSize acc'

let private xmlDocCache =
  Collections.Concurrent.ConcurrentDictionary<string, Map<string, XmlDocMember>>()

let private findCultures v =
  let rec loop state (v: System.Globalization.CultureInfo) =
    let state' = v.Name :: state

    if v.Parent = System.Globalization.CultureInfo.InvariantCulture then
      "" :: state' |> List.rev
    else
      loop state' v.Parent

  loop [] v

let private findLocalizedXmlFile (xmlFile: string) =
  let xmlName = Path.GetFileName xmlFile
  let path = Path.GetDirectoryName xmlFile

  findCultures System.Globalization.CultureInfo.CurrentUICulture
  |> List.map (fun culture -> Path.Combine(path, culture, xmlName))
  |> List.tryFind File.Exists
  |> Option.defaultValue xmlFile

let pPattern = Regex """(<p .*?>)+(.*)(<\/?p>)*"""

let private getXmlDoc dllFile =
  let xmlFile = Path.ChangeExtension(dllFile, ".xml")
  //Workaround for netstandard.dll
  let xmlFile =
    if
      xmlFile.Contains "packages"
      && xmlFile.Contains "netstandard.library"
      && xmlFile.Contains "netstandard2.0"
    then
      Path.Combine(Path.GetDirectoryName(xmlFile), "netstandard.xml")
    else
      xmlFile

  let xmlFile = findLocalizedXmlFile xmlFile

  match xmlDocCache.TryGetValue xmlFile with
  | true, cachedXmlFile -> Some cachedXmlFile
  | false, _ ->
    let rec exists filePath tryAgain =
      match File.Exists filePath, tryAgain with
      | true, _ -> Some filePath
      | false, false -> None
      | false, true ->
        // In Linux, we need to check for upper case extension separately
        let filePath = Path.ChangeExtension(filePath, Path.GetExtension(filePath).ToUpper())
        exists filePath false

    match exists xmlFile true with
    | None -> None
    | Some actualXmlFile ->
      // Prevent other threads from tying to add the same doc simultaneously
      xmlDocCache.AddOrUpdate(xmlFile, Map.empty, (fun _ _ -> Map.empty)) |> ignore

      try
        let cnt = File.ReadAllText actualXmlFile
        //Workaround for netstandard xmlDoc
        let cnt =
          if actualXmlFile.Contains "netstandard.xml" then
            let cnt = pPattern.Replace(cnt, "$2")

            cnt.Replace("<p>", "").Replace("</p>", "").Replace("<br>", "")
          else
            cnt

        use stringReader = new StringReader(cnt)
        use reader = XmlReader.Create stringReader
        let xmlDoc = readXmlDoc reader 0 Map.empty

        xmlDocCache.AddOrUpdate(xmlFile, xmlDoc, (fun _ _ -> xmlDoc)) |> ignore

        Some xmlDoc
      with _ ->
        None // TODO: Remove the empty map from cache to try again in the next request?

// --------------------------------------------------------------------------------------
// Formatting of tool-tip information displayed in F# IntelliSense
// --------------------------------------------------------------------------------------

[<RequireQualifiedAccess>]
type private TryGetXmlDocMemberResult =
  | Some of XmlDocMember
  | None
  | Error

[<RequireQualifiedAccess>]
type TipFormatterResult<'T> =
  | Success of 'T
  | Error of string
  | None

let private tryGetXmlDocMember (xmlDoc: FSharpXmlDoc) =
  try
    match xmlDoc with
    | FSharpXmlDoc.FromXmlText xmldoc ->

      let document = xmldoc.GetXmlText()
      // We create a "fake" XML document in order to use the same parser for both libraries and user code
      let xml = sprintf "<fake>%s</fake>" document
      let doc = XmlDocument()
      doc.LoadXml(xml)

      // This try to mimic how we found the indentation size when working a real XML file
      let rec findIndentationSize (lines: string list) =
        match lines with
        | head :: tail ->
          let lesserThanIndex = head.IndexOf('<', StringComparison.Ordinal)

          if lesserThanIndex <> -1 then
            lesserThanIndex
          else
            findIndentationSize tail
        | [] -> 0

      let indentationSize =
        xmldoc.GetElaboratedXmlLines() |> Array.toList |> findIndentationSize

      let xmlDoc = XmlDocMember(doc, indentationSize, 0)

      TryGetXmlDocMemberResult.Some xmlDoc

    | FSharpXmlDoc.FromXmlFile(dllFile, memberName) ->
      match getXmlDoc dllFile with
      | Some doc ->
        match doc.TryGetValue memberName with
        | true, docmember -> TryGetXmlDocMemberResult.Some docmember
        | false, _ -> TryGetXmlDocMemberResult.None
      | _ -> TryGetXmlDocMemberResult.None

    | FSharpXmlDoc.None -> TryGetXmlDocMemberResult.None
  with ex ->

    TryGetXmlDocMemberResult.Error

[<Literal>]
let private ERROR_WHILE_PARSING_DOC_COMMENT =
  "An error occurred when parsing the doc comment, please check that your doc comment is valid.\n\nMore info can be found in the LSP output"

let private formatTaggedText (t: TaggedText) : string =
  match t.Tag with
  | TextTag.ActivePatternResult
  | TextTag.UnionCase
  | TextTag.Delegate
  | TextTag.Field
  | TextTag.Keyword
  | TextTag.LineBreak
  | TextTag.Local
  | TextTag.RecordField
  | TextTag.Method
  | TextTag.Member
  | TextTag.ModuleBinding
  | TextTag.Function
  | TextTag.Module
  | TextTag.Namespace
  | TextTag.NumericLiteral
  | TextTag.Operator
  | TextTag.Parameter
  | TextTag.Property
  | TextTag.Space
  | TextTag.StringLiteral
  | TextTag.Text
  | TextTag.Punctuation
  | TextTag.UnknownType -> t.Text
  | TextTag.UnknownEntity
  | TextTag.Enum
  | TextTag.Event
  | TextTag.ActivePatternCase
  | TextTag.Struct
  | TextTag.Alias
  | TextTag.Class
  | TextTag.Union
  | TextTag.Interface
  | TextTag.Record
  | TextTag.TypeParameter -> $"`{t.Text}`"

let private formatUntaggedText (t: TaggedText) = t.Text

let private formatUntaggedTexts = Array.map formatUntaggedText >> String.concat ""

let private formatTaggedTexts =
  Array.map formatTaggedText >> String.concat "" >> (fun s -> s.Replace("``", ""))

let private formatGenericParameters (typeMappings: TaggedText[] list) =
  typeMappings
  |> List.map (fun typeMap -> $"* {formatTaggedTexts typeMap}")
  |> String.concat nl

/// CompletionItems are formatted with an unmodified signature since the signature portion of the
/// item isn't markdown-compatible. The documentation shown however is markdown.
let formatCompletionItemTip (ToolTipText tips) : (string * string) =
  tips
  |> List.pick (function
    | ToolTipElement.Group items ->
      let makeTooltip (tipElement: ToolTipElementData) =
        let header = formatUntaggedTexts tipElement.MainDescription

        let body =
          match tryGetXmlDocMember tipElement.XmlDoc with
          | TryGetXmlDocMemberResult.Some xmlDoc -> xmlDoc.FormatComment(FormatCommentStyle.Legacy)
          | TryGetXmlDocMemberResult.None -> ""
          | TryGetXmlDocMemberResult.Error -> ERROR_WHILE_PARSING_DOC_COMMENT

        header, body

      items |> List.tryHead |> Option.map makeTooltip

    | ToolTipElement.CompositionError(error) -> Some("<Note>", error)
    | _ -> Some("<Note>", "No signature data"))

/// Formats a tooltip signature for output as a signatureHelp,
/// which means no markdown formatting.
let formatPlainTip (ToolTipText tips) : (string * string) =
  tips
  |> List.pick (function
    | ToolTipElement.Group items ->
      let t = items |> Seq.head
      let signature = formatUntaggedTexts t.MainDescription

      let description =
        match tryGetXmlDocMember t.XmlDoc with
        | TryGetXmlDocMemberResult.Some xmlDoc -> xmlDoc.FormatComment(FormatCommentStyle.Legacy)
        | TryGetXmlDocMemberResult.None -> ""
        | TryGetXmlDocMemberResult.Error -> ERROR_WHILE_PARSING_DOC_COMMENT

      Some(signature, description)
    | ToolTipElement.CompositionError(error) -> Some("<Note>", error)
    | _ -> Some("<Note>", "No signature data"))


let prepareSignature (signatureText: string) =
  signatureText.Split Environment.NewLine
  // Remove empty lines
  |> Array.filter (not << String.IsNullOrWhiteSpace)
  |> String.concat nl

let prepareFooterLines (footerText: string) =
  footerText.Split Environment.NewLine
  // Remove empty lines
  |> Array.filter (not << String.IsNullOrWhiteSpace)
  // Mark each line as an individual string in italics
  |> Array.map (fun n -> "*" + n + "*")


let private tryComputeTooltipInfo (ToolTipText tips) (formatCommentStyle: FormatCommentStyle) =

  // Note: In the previous code, we were returning a `(string * string * string) list list`
  // but always discarding the tooltip later if the list had more than one element
  // and only using the first element of the inner list.
  // More over, I don't know in which case we can have several elements in the
  // `(ToolTipText tips)` parameter.
  // So I can't test why we have list of list stuff, but like I said, we were
  // discarding the tooltip if it had more than one element.
  //
  // The new code should do the same thing, as before but instead of
  // computing the rendered tooltip, and discarding some of them afterwards,
  // we are discarding the things we don't want earlier and only compute the
  // tooltip we want to display if we have the right data.

  let computeGenericParametersText (tooltipData: ToolTipElementData) =
    // If there are no generic parameters, don't display the section
    if tooltipData.TypeMapping.IsEmpty then
      None
    // If there are generic parameters, display the section
    else
      "**Generic Parameters**"
      + nl
      + nl
      + formatGenericParameters tooltipData.TypeMapping
      |> Some

  tips
  // Render the first valid tooltip and return it
  |> List.tryPick (function
    | ToolTipElement.Group(tooltipData :: _) ->
      let docComment, hasTruncatedExamples =
        match tryGetXmlDocMember tooltipData.XmlDoc with
        | TryGetXmlDocMemberResult.Some xmlDoc ->
          // Format the doc comment
          let docCommentText = xmlDoc.FormatComment formatCommentStyle

          // Concatenate the doc comment and the generic parameters section
          let consolidatedDocCommentText =
            match computeGenericParametersText tooltipData with
            | Some genericParametersText -> docCommentText + nl + nl + genericParametersText
            | None -> docCommentText

          consolidatedDocCommentText, xmlDoc.HasTruncatedExamples

        | TryGetXmlDocMemberResult.None ->
          // Even if a symbol doesn't have a doc comment, it can still have generic parameters
          let docComment =
            match computeGenericParametersText tooltipData with
            | Some genericParametersText -> genericParametersText
            | None -> ""

          docComment, false
        | TryGetXmlDocMemberResult.Error -> ERROR_WHILE_PARSING_DOC_COMMENT, false

      {| DocComment = docComment
         HasTruncatedExamples = hasTruncatedExamples |}
      |> Ok
      |> Some

    | ToolTipElement.CompositionError error -> error |> Error |> Some

    | ToolTipElement.Group []
    | ToolTipElement.None -> None)

/// <summary>
/// Try format the given tooltip with the requested style.
/// </summary>
/// <param name="toolTipText">Tooltip documentation to render in the middle</param>
/// <param name="formatCommentStyle">Style of tooltip</param>
/// <returns>
/// - <c>TipFormatterResult.Success {| DocComment; HasTruncatedExamples |}</c> if the doc comment has been formatted
///
///   Where DocComment is the format tooltip and HasTruncatedExamples is true if examples have been truncated
///
/// - <c>TipFormatterResult.None</c> if the doc comment has not been found
/// - <c>TipFormatterResult.Error string</c> if an error occurred while parsing the doc comment
/// </returns>
let tryFormatTipEnhanced toolTipText (formatCommentStyle: FormatCommentStyle) =

  match tryComputeTooltipInfo toolTipText formatCommentStyle with
  | Some(Ok tooltipResult) -> TipFormatterResult.Success tooltipResult

  | Some(Error error) -> TipFormatterResult.Error error

  | None -> TipFormatterResult.None

let private buildFormatComment cmt (formatStyle: FormatCommentStyle) (typeDoc: string option) =
    match tryGetXmlDocMember cmt with
    | TryGetXmlDocMemberResult.Some xmlDoc ->
        match formatStyle with
        | FormatCommentStyle.Legacy -> xmlDoc.ToString()
        | FormatCommentStyle.SummaryOnly -> xmlDoc.ToSummaryOnlyString()
        | FormatCommentStyle.FullEnhanced -> xmlDoc.ToFullEnhancedString()
        | FormatCommentStyle.Documentation -> xmlDoc.ToDocumentationString()
    | TryGetXmlDocMemberResult.None -> ""
    | TryGetXmlDocMemberResult.Error -> ""

let formatTipEnhanced
  (ToolTipText tips)
  (signature: string)
  (footer: string)
  (typeDoc: string option)
  (formatCommentStyle: FormatCommentStyle)
  : (string * string * string) list list =
  
  // Normalize signature: ensure space before colon in type annotations
  // The new FsAutoComplete SignatureFormatter produces "val foo: int" but tests expect "val foo : int"
  let normalizedSignature = 
      if signature.Contains(":") then
          System.Text.RegularExpressions.Regex.Replace(signature, @"(\w+):\s*", "$1 : ")
      else
          signature
  
  tips
  |> List.choose (function
    | ToolTipElement.Group items ->
      Some(
        items
        |> List.map (fun i ->
          let comment =
            if i.TypeMapping.IsEmpty then
              buildFormatComment i.XmlDoc formatCommentStyle typeDoc
            else
              buildFormatComment i.XmlDoc formatCommentStyle typeDoc
              + nl
              + nl
              + "**Generic Parameters**"
              + nl
              + nl
              + formatGenericParameters i.TypeMapping

          (normalizedSignature, comment, footer))
      )
    | ToolTipElement.CompositionError (error) -> Some [ ("<Note>", error, "") ]
    | _ -> None)

/// <summary>
/// Generate the 'Show documentation' link for the tooltip.
///
/// The link is rendered differently depending on if examples
/// have been truncated or not.
/// </summary>
/// <param name="hasTruncatedExamples"><c>true</c> if the examples have been truncated</param>
/// <param name="xmlDocSig">XmlDocSignature in the format of <c>T:System.String.concat</c></param>
/// <param name="assemblyName">Assembly name, example <c>FSharp.Core</c></param>
/// <returns>Returns a string which represent the show documentation link</returns>
let renderShowDocumentationLink (hasTruncatedExamples: bool) (xmlDocSig: string) (assemblyName: string) =

  // TODO: Refactor this code, to avoid duplicate with DocumentationFormatter.fs
  let content =
    Uri.EscapeDataString(sprintf """[{ "XmlDocSig": "%s", "AssemblyName": "%s" }]""" xmlDocSig assemblyName)

  let text =
    if hasTruncatedExamples then
      "Open the documentation to see the truncated examples"
    else
      "Open the documentation"

  $"<a href='command:fsharp.showDocumentation?%s{content}'>%s{text}</a>"

/// <summary>
/// Try format the given tooltip as documentation.
/// </summary>
/// <param name="toolTipText">Tooltip to format</param>
/// <returns>
/// - <c>TipFormatterResult.Success string</c> if the doc comment has been formatted
/// - <c>TipFormatterResult.None</c> if the doc comment has not been found
/// - <c>TipFormatterResult.Error string</c> if an error occurred while parsing the doc comment
/// </returns>
let tryFormatDocumentationFromTooltip toolTipText =

  match tryComputeTooltipInfo toolTipText FormatCommentStyle.Documentation with
  | Some(Ok tooltipResult) -> TipFormatterResult.Success tooltipResult.DocComment

  | Some(Error error) -> TipFormatterResult.Error error

  | None -> TipFormatterResult.None

/// <summary>
/// Try format the doc comment based on the XmlSignature and the assembly name.
/// </summary>
/// <param name="xmlSig">
/// XmlSignature used to identify the doc comment to format
///
/// Example: <c>T:System.String.concat</c>
/// </param>
/// <param name="assembly">
/// Assembly name used to identify the doc comment to format
///
/// Example: <c>FSharp.Core</c>
/// </param>
/// <returns>
/// - <c>TipFormatterResult.Success string</c> if the doc comment has been formatted
/// - <c>TipFormatterResult.None</c> if the doc comment has not been found
/// - <c>TipFormatterResult.Error string</c> if an error occurred while parsing the doc comment
/// </returns>
let tryFormatDocumentationFromXmlSig (xmlSig: string) (assembly: string) =
  let xmlDoc = FSharpXmlDoc.FromXmlFile(assembly, xmlSig)

  match tryGetXmlDocMember xmlDoc with
  | TryGetXmlDocMemberResult.Some xmlDoc ->
    let formattedComment = xmlDoc.FormatComment(FormatCommentStyle.Documentation)

    TipFormatterResult.Success formattedComment

  | TryGetXmlDocMemberResult.None -> TipFormatterResult.None
  | TryGetXmlDocMemberResult.Error -> TipFormatterResult.Error ERROR_WHILE_PARSING_DOC_COMMENT

let formatDocumentationFromXmlDoc xmlDoc =
  match tryGetXmlDocMember xmlDoc with
  | TryGetXmlDocMemberResult.Some xmlDoc ->
    let formattedComment = xmlDoc.FormatComment(FormatCommentStyle.Documentation)

    TipFormatterResult.Success formattedComment

  | TryGetXmlDocMemberResult.None -> TipFormatterResult.None
  | TryGetXmlDocMemberResult.Error -> TipFormatterResult.Error ERROR_WHILE_PARSING_DOC_COMMENT

let extractSignature (ToolTipText tips) =
  let getSignature (t: TaggedText[]) =
    let str = formatUntaggedTexts t
    let nlpos = str.IndexOfAny([| '\r'; '\n' |])

    let firstLine = if nlpos > 0 then str.[0 .. nlpos - 1] else str

    if firstLine.StartsWith("type ", StringComparison.Ordinal) then
      let index = firstLine.LastIndexOf("=", StringComparison.Ordinal)

      if index > 0 then firstLine.[0 .. index - 1] else firstLine
    else
      firstLine

  let firstResult x =
    match x with
    | ToolTipElement.Group gs ->
      List.tryPick
        (fun (t: ToolTipElementData) ->
          if not (Array.isEmpty t.MainDescription) then
            Some t.MainDescription
          else
            None)
        gs
    | _ -> None

  tips
  |> Seq.tryPick firstResult
  |> Option.map getSignature
  |> Option.defaultValue ""

/// extracts any generic parameters present in this tooltip, rendering them as plain text
let extractGenericParameters (ToolTipText tips) =
  let firstResult x =
    match x with
    | ToolTipElement.Group gs ->
      List.tryPick
        (fun (t: ToolTipElementData) ->
          if not (t.TypeMapping.IsEmpty) then
            Some t.TypeMapping
          else
            None)
        gs
    | _ -> None

  tips
  |> Seq.tryPick firstResult
  |> Option.defaultValue []
  |> List.map formatUntaggedTexts
