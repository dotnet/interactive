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
    [ "r", "References an assembly"
      "load", "Reads a source file, compiles it, and runs it."
      "I", "Specifies an assembly search path in quotation marks."
      "light", "Enables or disables lightweight syntax, for compatibility with other versions of ML"
      "if", "Supports conditional compilation"
      "else", "Supports conditional compilation"
      "endif", "Supports conditional compilation"
      "nowarn", "Disables a compiler warning or warnings"
      "line", "Indicates the original source code line" ]
    |> dict
