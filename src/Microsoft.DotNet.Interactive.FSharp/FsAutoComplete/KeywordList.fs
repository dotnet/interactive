namespace FsAutoComplete

open FSharp.Compiler.SourceCodeServices

#nowarn "57"

module internal KeywordList =

    let keywordDescriptions =
        FSharp.Compiler.SourceCodeServices.FSharpKeywords.KeywordsWithDescription
        |> dict

    let keywordTooltips =
      keywordDescriptions
      |> Seq.map (fun kv ->
        let tip = FSharpToolTipText [FSharpToolTipElement.Single(kv.Key, FSharpXmlDoc.Text ([|kv.Value|], [|""|]))]
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
        FSharp.Compiler.SourceCodeServices.FSharpKeywords.KeywordsWithDescription
        |> List.map fst
