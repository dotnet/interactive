namespace FsAutoComplete

open FSharp.Compiler.Text
open FSharp.Compiler.Tokenization
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols

module KeywordList =

  let keywordDescriptions = FSharpKeywords.KeywordsWithDescription |> dict

  let keywordTooltips =
    keywordDescriptions
    |> Seq.map (fun kv ->
      let lines = kv.Value.Replace("\r\n", "\n").Split('\n')

      let allLines = Array.concat [| [| "<summary>" |]; lines; [| "</summary>" |] |]

      let tip =
        ToolTipText
          [ ToolTipElement.Single(
              [| TaggedText.tagText kv.Key |],
              FSharpXmlDoc.FromXmlText(FSharp.Compiler.Xml.XmlDoc(allLines, Range.range0))
            ) ]

      kv.Key, tip)
    |> dict

  let hashDirectives =
    [ "r", "References an assembly or a nuget: package"
      "load", "References a source .fsx script or .fs file, by compiling and running it."
      "I", "Specifies an assembly search path in quotation marks."
      "light", "Enables or disables lightweight syntax, for compatibility with other versions of ML"
      "if", "Supports conditional compilation"
      "else", "Supports conditional compilation"
      "endif", "Supports conditional compilation"
      "nowarn", "Disables a compiler warning or warnings"
      "warnon", "Enables a compiler warning or warnings"
      "quit", "exits the interactive session"
      "time", "toggles whether to display performance information"
      "line", "Indicates the original source code line" ]
    |> dict

  let allKeywords: string list =
    keywordDescriptions |> Seq.map ((|KeyValue|) >> fst) |> Seq.toList
