// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.Tests

open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html
open Microsoft.VisualStudio.TestTools.UnitTesting

type HtmlFormatterTests() =

    let PlainTextBegin = "<div class=\"dni-plaintext\">";
    let PlainTextEnd = "</div>";

    [<TestMethod>]
    member __.``empty tag``() =
        Assert.AreEqual<string>("<div></div>", (div [] []).ToString())

    [<TestMethod>]
    member __.``indexer as attribute``() =
        Assert.AreEqual<string>("<div class=\"c\"></div>", (div [_class "c"] []).ToString());

    [<TestMethod>]
    member __.``HTML from string``() =
        Assert.AreEqual<string>("<div>d</div>", (div [] [ "d" ]).ToString())

    [<TestMethod>]
    // Note, the inner object is currently rendered using plaintext formatting
    member __.``HTML from inner object``() =
        Assert.AreEqual<string>("<div>11</div>", (div [] [str (string 11)]).ToString())

    [<TestMethod>]
    member __.``HTML from inner object that is ScriptContent``() =
        Assert.AreEqual<string>("<script>var x = 1 < 2;</script>", (script [] [ScriptContent ("var x = 1 < 2;")]).ToString())

    [<TestMethod>]
    // Note, this test result will change in the future once F# formatting uses %A 
    // formatting by default for plaintext display
    member __.``HTML from inner object rendered as plaintext with encoded characters``() =
        Assert.AreEqual<string>(sprintf "<div>[ &gt;, &lt; ]</div>", (div [] [  "[ >, < ]" ]).ToString())

    [<TestMethod>]
    member __.``HTML from content with attribute``() =
        Assert.AreEqual<string>("<div class=\"c\">d</div>", (div [_class "c"] [str "d"]).ToString())

    [<TestMethod>]
    member __.``HTML from another tag``() =
        Assert.AreEqual<string>("<div><a>foo</a></div>", (div [] [a [] [str "foo"]]).ToString())

    [<TestMethod>]
    member __.``HTML varargs 0``() =
        Assert.AreEqual<string>("<div></div>", (div [] [] ).ToString())

    [<TestMethod>]
    member __.``HTML varargs 2``() =
        Assert.AreEqual<string>("<div>ab</div>", (div [] [str "a"; str "b"]).ToString())

    [<TestMethod>]
    member __.``Formatting _style attribute value`` () =
        Assert.AreEqual((_style ["a"; " b; "; "c;"]), HtmlAttribute ("style", "a; b; c"))
        Assert.AreEqual(_style [
            "width: 3em;";
            "background: rgb(0,0,0);";
            "display: inline-block;";
            "border: 3px solid black;";
        ], HtmlAttribute ("style", "width: 3em; background: rgb(0,0,0); display: inline-block; border: 3px solid black"))