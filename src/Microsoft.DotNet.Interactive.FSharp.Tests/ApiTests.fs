// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.Tests

open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Formatting
open Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html
open Xunit

type ApiTests() =

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
        Assert.Equal("<div>d</div>", (div [] [str "d"]).ToString())

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
    member __.``HTML from object rendered as plaintext``() =
        Assert.Equal(sprintf "<div>%s[ 1, 2 ]%s</div>" PlainTextBegin PlainTextEnd, (div [] [embed (FormatContext()) "[ 1, 2 ]"]).ToString())

    [<Fact>]
    // Note, this test result will change in the future once F# formatting uses %A 
    // formatting by default for plaintext display
    member __.``HTML from inner object rendered as plaintext with encoded characters``() =
        Assert.Equal(sprintf "<div>%s[ &gt;, &lt; ]%s</div>" PlainTextBegin PlainTextEnd, (div [] [embed (FormatContext()) "[ >, < ]" ]).ToString())

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
