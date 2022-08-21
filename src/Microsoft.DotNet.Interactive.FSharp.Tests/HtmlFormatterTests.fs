﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.Tests

open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html
open Xunit

type HtmlFormatterTests() =

    let PlainTextBegin = "<div class=\"dni-plaintext\">";
    let PlainTextEnd = "</div>";

    [<Fact>]
    member __.``empty tag``() =
        Assert.Equal("<div></div>", (div [] []).ToString())

    [<Fact>]
    member __.``indexer as attribute``() =
        Assert.Equal("<div class=\"c\"></div>", (div [_class "c"] []).ToString());

    [<Fact>]
    member __.``HTML from string``() =
        Assert.Equal("<div>d</div>", (div [] [ "d" ]).ToString())

    [<Fact>]
    // Note, the inner object is currently rendered using plaintext formatting
    member __.``HTML from inner object``() =
        Assert.Equal("<div>11</div>", (div [] [str (string 11)]).ToString())

    [<Fact>]
    member __.``HTML from inner object that is ScriptContent``() =
        Assert.Equal("<script>var x = 1 < 2;</script>", (script [] [ScriptContent ("var x = 1 < 2;")]).ToString())

    [<Fact>]
    // Note, this test result will change in the future once F# formatting uses %A 
    // formatting by default for plaintext display
    member __.``HTML from inner object rendered as plaintext with encoded characters``() =
        Assert.Equal(sprintf "<div>[ &gt;, &lt; ]</div>", (div [] [  "[ >, < ]" ]).ToString())

    [<Fact>]
    member __.``HTML from content with attribute``() =
        Assert.Equal("<div class=\"c\">d</div>", (div [_class "c"] [str "d"]).ToString())

    [<Fact>]
    member __.``HTML from another tag``() =
        Assert.Equal("<div><a>foo</a></div>", (div [] [a [] [str "foo"]]).ToString())

    [<Fact>]
    member __.``HTML varargs 0``() =
        Assert.Equal("<div></div>", (div [] [] ).ToString())

    [<Fact>]
    member __.``HTML varargs 2``() =
        Assert.Equal("<div>ab</div>", (div [] [str "a"; str "b"]).ToString())

    [<Fact>]
    member __.``Formatting _style attribute value`` () =
        Assert.Equal((_style ["a"; " b; "; "c;"]), HtmlAttribute ("style", "a; b; c"))
        Assert.Equal(_style [
            "width: 3em;";
            "background: rgb(0,0,0);";
            "display: inline-block;";
            "border: 3px solid black;";
        ], HtmlAttribute ("style", "width: 3em; background: rgb(0,0,0); display: inline-block; border: 3px solid black"))