namespace FsAutoComplete

open FSharp.Compiler.EditorServices
open FSharp.Compiler.Text
open FSharp.Compiler.Xml
open FSharp.Compiler.Tokenization
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax

#nowarn "57"

module internal KeywordList =

    let keywordDescriptions =
        FSharpKeywords.KeywordsWithDescription
        |> dict

    let keywordTooltips =
      keywordDescriptions
      |> Seq.map (fun kv ->
        let tip = ToolTipText[ ToolTipElement.Single( [| TaggedText(TextTag.Keyword, kv.Key) |], FSharpXmlDoc.FromXmlText (XmlDoc([|kv.Value|], Range.rangeStartup))) ]
        kv.Key, tip)
      |> dict

    let hashDirectives =
        [ "r", "References an assembly"
          "load", "Reads a source file, compiles it, and runs it."
          "I", "Specifies an assembly search path in quotation marks."
          "light", "Enables or disables lightweight syntax, for compatibility with other versions of ML"
          "if", "Supports conditional compilation"
          "else", "Supports conditional compilation"
          "endif", "Supports conditional compilation"
          "nowarn", "Disables a compiler warning or warnings"
          "line", "Indicates the original source code line"]
        |> dict


    let allKeywords : string list =
        FSharpKeywords.KeywordsWithDescription
        |> List.map fst
