// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.Tests

open Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html
open Xunit

type ApiTests() =

    [<Fact>]
    member __.``empty tag``() =
        Assert.Equal("<div></div>", (div [] []).ToString())

    [<Fact>]
    member __.``indexer as attribute``() =
        Assert.Equal("<div class=\"c\"></div>", (div [_class "c"] []).ToString());

    [<Fact>]
    member __.``inner HTML from string``() =
        Assert.Equal("<div>d</div>", (div [] [str "d"]).ToString())

    [<Fact>]
    member __.``inner HTML from object``() =
        Assert.Equal("<div>11</div>", (div [] [object 11]).ToString())

    [<Fact>]
    member __.``inner HTML from content with attribute``() =
        Assert.Equal("<div class=\"c\">d</div>", (div [_class "c"] [str "d"]).ToString())

    [<Fact>]
    member __.``inner HTML from another tag``() =
        Assert.Equal("<div><a>foo</a></div>", (div [] [a [] [str "foo"]]).ToString())

    [<Fact>]
    member __.``inner HTML varargs 0``() =
        Assert.Equal("<div></div>", (div [] [] ).ToString())

    [<Fact>]
    member __.``inner HTML varargs 2``() =
        Assert.Equal("<div>ab</div>", (div [] [str "a"; object "b"]).ToString())
