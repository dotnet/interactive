// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.Tests

open Microsoft.DotNet.Interactive.FSharp.FSharpPocketViewTags
open Xunit

type ApiTests() =

    [<Fact>]
    member __.``empty tag``() =
        Assert.Equal("<div></div>", div.ToString())

    [<Fact>]
    member __.``indexer as attribute``() =
        Assert.Equal("<div class=\"c\"></div>", div.["class", "c"].ToString());

    [<Fact>]
    member __.``inner HTML from content``() =
        Assert.Equal("<div>d</div>", div.innerHTML("d").ToString())

    [<Fact>]
    member __.``inner HTML from content with attribute``() =
        Assert.Equal("<div class=\"c\">d</div>", div.["class", "c"].innerHTML("d").ToString())

    [<Fact>]
    member __.``inner HTML from another tag``() =
        Assert.Equal("<div><a>foo</a></div>", div.innerHTML(a.innerHTML("foo")).ToString())

    [<Fact>]
    member __.``inner HTML varargs 0``() =
        Assert.Equal("<div></div>", div.innerHTML().ToString())

    [<Fact>]
    member __.``inner HTML varargs 2``() =
        Assert.Equal("<div>ab</div>", div.innerHTML("a", "b").ToString())
