// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open Microsoft.AspNetCore.Html
open Microsoft.DotNet.Interactive.Formatting

type FSharpPocketViewTags(p: PocketView) as this =
    override __.ToString() = p.ToString()
    abstract member innerHTML: [<ParamArray>] content: obj[] -> FSharpPocketViewTags
    abstract member Item: string * obj -> FSharpPocketViewTags with get
    default __.innerHTML content =
        p.SetContent(content)
        this
    default __.Item
        with get (attribute: string, value: obj) =
            p.HtmlAttributes.[attribute] <- value
            this
    interface IHtmlContent with
        member __.WriteTo(writer, encoder) = p.WriteTo(writer, encoder)

// This class exists because F# can't open static classes like it can modules.  All items on the `FSharpPocketViewTags`
// module below are essentially fields that are initialized at runtime start, which means that there is only one
// instance of the `div` item.  Since calling the indexer to set attributes or the `innerHTML()` method to set the
// content mutates the inner value, we need a way to get around that.  The fix is for this class to exist at the root
// level, but as soon as a mutating method is called, a clone is made.  The base type is kept the same to simplify the
// public API surface area.
type internal FSharpPocketViewTagsRoot(p: PocketView) =
    inherit FSharpPocketViewTags(p)
    let wrapped () = FSharpPocketViewTags(PocketView(tagName=p.Name, nested=p))
    override __.ToString() = p.ToString()
    override __.innerHTML content = wrapped().innerHTML(content)
    override __.Item
        with get (attribute: string, value: obj) = wrapped().[attribute, value]

module FSharpPocketViewTags =
    let private f name = FSharpPocketViewTagsRoot(PocketView(tagName=name)) :> FSharpPocketViewTags
    let a = f "a"
    let area = f "area"
    let aside = f "aside"
    let b = f "b"
    let body = f "body"
    let br = f "br"
    let button = f "button"
    let caption = f "caption"
    let center = f "center"
    let code = f "code"
    let colgroup = f "colgroup"
    let dd = f "dd"
    let details = f "details"
    let div = f "div"
    let dl = f "dl"
    let dt = f "dt"
    let em = f "em"
    let figure = f "figure"
    let font = f "font"
    let form = f "form"
    let h1 = f "h1"
    let h2 = f "h2"
    let h3 = f "h3"
    let h4 = f "h4"
    let h5 = f "h5"
    let h6 = f "h6"
    let head = f "head"
    let header = f "header"
    let hgroup = f "hgroup"
    let hr = f "hr"
    let html = f "html"
    let i = f "i"
    let iframe = f "iframe"
    let img = f "img"
    let input = f "input"
    let label = f "label"
    let li = f "li"
    let link = f "link"
    let main = f "main"
    let menu = f "menu"
    let menuitem = f "menuitem"
    let meta = f "meta"
    let meter = f "meter"
    let nav = f "nav"
    let ol = f "ol"
    let optgroup = f "optgroup"
    let option = f "option"
    let p = f "p"
    let pre = f "pre"
    let progress = f "progress"
    let q = f "q"
    let script = f "script"
    let section = f "section"
    let select = f "select"
    let small = f "small"
    let source = f "source"
    let span = f "span"
    let strike = f "strike"
    let style = f "style"
    let strong = f "strong"
    let sub = f "sub"
    let sup = f "sup"
    let svg = f "svg"
    let table = f "table"
    let tbody = f "tbody"
    let td = f "td"
    let textarea = f "textarea"
    let tfoot = f "tfoot"
    let th = f "th"
    let thead = f "thead"
    let title = f "title"
    let tr = f "tr"
    let u = f "u"
    let ul = f "ul"
    let video = f "video"
