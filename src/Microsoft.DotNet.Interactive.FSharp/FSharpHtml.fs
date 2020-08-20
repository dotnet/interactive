﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers

open Microsoft.AspNetCore.Html
open Microsoft.DotNet.Interactive.Formatting

module Html = 

    type internal HtmlElement =
        | Tagged of PocketView
        | Text of string
        | Obj of obj

        override x.ToString() = 
            match x with 
            | Tagged p -> p.ToString()
            | Text s -> s
            | Obj o -> o.ToString()

        interface IHtmlContent with
            member p.WriteTo(writer, encoder) = 
                match p with 
                | Text s -> encoder.Encode(writer, s)
                | Tagged  p -> p.WriteTo(writer, encoder)
                | Obj o -> encoder.Encode(writer, string o)

    type HtmlAttribute = string * obj

    [<AutoOpen>]
    module HtmlElements =
        type internal IMarker = interface end
        let private makeElement name (attrs: HtmlAttribute list) (content: IHtmlContent list) = 
            let p = new PocketView(tagName=name)
            for (tag, value) in attrs do
                p.HtmlAttributes.[tag] <- value
            match content with 
            | [] -> ()
            | _ -> 
                let content = 
                    content 
                    |> List.toArray 
                    |> Array.map (fun content -> 
                         match content with 
                         | :? HtmlElement as s -> 
                            // Prefer to pass the object over for 
                            match s with 
                            | Obj o -> o
                            | _ -> box s
                         | _ -> box content)
                p.SetContent(content)
            (HtmlElement.Tagged p :> IHtmlContent)

        /// Specifies an HTML element
        let a attrs content = makeElement "a" attrs content
        /// Specifies an HTML element
        let area attrs content = makeElement "area" attrs content
        /// Specifies an HTML element
        let aside attrs content = makeElement "aside" attrs content
        /// Specifies an HTML element
        let b attrs content = makeElement "b" attrs content
        /// Specifies an HTML element
        let br attrs content = makeElement "br" attrs content
        /// Specifies an HTML element
        let button attrs content = makeElement "button" attrs content
        /// Specifies an HTML element
        let caption attrs content = makeElement "caption" attrs content
        /// Specifies an HTML element
        let center attrs content = makeElement "center" attrs content
        /// Specifies an HTML element
        let code attrs content = makeElement "code" attrs content
        /// Specifies an HTML element
        let colgroup attrs content = makeElement "colgroup" attrs content
        /// Specifies an HTML element
        let dd attrs content = makeElement "dd" attrs content
        /// Specifies an HTML element
        let details attrs content = makeElement "details" attrs content
        /// Specifies an HTML element
        let div attrs content = makeElement "div" attrs content
        /// Specifies an HTML element
        let dl attrs content = makeElement "dl" attrs content
        /// Specifies an HTML element
        let dt attrs content = makeElement "dt" attrs content
        /// Specifies an HTML element
        let em attrs content = makeElement "em" attrs content
        /// Specifies an HTML element
        let figure attrs content = makeElement "figure" attrs content
        /// Specifies an HTML element
        let font attrs content = makeElement "font" attrs content
        /// Specifies an HTML element
        let form attrs content = makeElement "form" attrs content
        /// Specifies an HTML element
        let h1 attrs content = makeElement "h1" attrs content
        /// Specifies an HTML element
        let h2 attrs content = makeElement "h2" attrs content
        /// Specifies an HTML element
        let h3 attrs content = makeElement "h3" attrs content
        /// Specifies an HTML element
        let h4 attrs content = makeElement "h4" attrs content
        /// Specifies an HTML element
        let h5 attrs content = makeElement "h5" attrs content
        /// Specifies an HTML element
        let h6 attrs content = makeElement "h6" attrs content
        /// Specifies an HTML element
        let head attrs content = makeElement "head" attrs content
        /// Specifies an HTML element
        let header attrs content = makeElement "header" attrs content
        /// Specifies an HTML element
        let hgroup attrs content = makeElement "hgroup" attrs content
        /// Specifies an HTML element
        let hr attrs content = makeElement "hr" attrs content

        // we skip thse because they are not commonly needed in display
        // specifications. 
        // let body attrs content = makeElement "body" attrs content
        // let html attrs content = makeElement "html" attrs content

        /// Specifies an HTML element
        let i attrs content = makeElement "i" attrs content
        /// Specifies an HTML element
        let iframe attrs content = makeElement "iframe" attrs content
        /// Specifies an HTML element
        let img attrs content = makeElement "img" attrs content
        /// Specifies an HTML element
        let input attrs content = makeElement "input" attrs content
        /// Specifies an HTML element
        let label attrs content = makeElement "label" attrs content
        /// Specifies an HTML element
        let li attrs content = makeElement "li" attrs content
        /// Specifies an HTML element
        let link attrs content = makeElement "link" attrs content
        /// Specifies an HTML element
        let main attrs content = makeElement "main" attrs content
        /// Specifies an HTML element
        let menu attrs content = makeElement "menu" attrs content
        /// Specifies an HTML element
        let menuitem attrs content = makeElement "menuitem" attrs content
        /// Specifies an HTML element
        let meta attrs content = makeElement "meta" attrs content
        /// Specifies an HTML element
        let meter attrs content = makeElement "meter" attrs content
        /// Specifies an HTML element
        let nav attrs content = makeElement "nav" attrs content
        /// Specifies an HTML element
        let ol attrs content = makeElement "ol" attrs content
        /// Specifies an HTML element
        let optgroup attrs content = makeElement "optgroup" attrs content
        /// Specifies an HTML element
        let option attrs content = makeElement "option" attrs content
        /// Specifies an HTML element
        let p attrs content = makeElement "p" attrs content
        /// Specifies an HTML element
        let pre attrs content = makeElement "pre" attrs content
        /// Specifies an HTML element
        let progress attrs content = makeElement "progress" attrs content
        /// Specifies an HTML element
        let q attrs content = makeElement "q" attrs content
        /// Specifies an HTML element
        let script attrs content = makeElement "script" attrs content
        /// Specifies an HTML element
        let section attrs content = makeElement "section" attrs content
        /// Specifies an HTML element
        let select attrs content = makeElement "select" attrs content
        /// Specifies an HTML element
        let small attrs content = makeElement "small" attrs content
        /// Specifies an HTML element
        let source attrs content = makeElement "source" attrs content
        /// Specifies an HTML element
        let span attrs content = makeElement "span" attrs content
        /// Specifies an HTML element
        let strike attrs content = makeElement "strike" attrs content
        /// Specifies an HTML element
        let style attrs content = makeElement "style" attrs content
        /// Specifies an HTML element
        let strong attrs content = makeElement "strong" attrs content
        /// Specifies an HTML element
        let sub attrs content = makeElement "sub" attrs content
        /// Specifies an HTML element
        let sup attrs content = makeElement "sup" attrs content
        /// Specifies an HTML element
        let svg attrs content = makeElement "svg" attrs content
        /// Specifies an HTML element
        let table attrs content = makeElement "table" attrs content
        /// Specifies an HTML element
        let tbody attrs content = makeElement "tbody" attrs content
        /// Specifies an HTML element
        let td attrs content = makeElement "td" attrs content
        /// Specifies an HTML element
        let textarea attrs content = makeElement "textarea" attrs content
        /// Specifies an HTML element
        let tfoot attrs content = makeElement "tfoot" attrs content
        /// Specifies an HTML element
        let th attrs content = makeElement "th" attrs content
        /// Specifies an HTML element
        let thead attrs content = makeElement "thead" attrs content
        /// Specifies an HTML element
        let title attrs content = makeElement "title" attrs content
        /// Specifies an HTML element
        let tr attrs content = makeElement "tr" attrs content
        /// Specifies an HTML element
        let u attrs content = makeElement "u" attrs content
        /// Specifies an HTML element
        let ul attrs content = makeElement "ul" attrs content
        /// Specifies an HTML element
        let video attrs content = makeElement "video" attrs content

        /// Specifies an HTML element that is the encoded text
        let encodedText (s: string) = (HtmlElement.Text(s) :> IHtmlContent)

        /// Specifies an HTML element that is the encoded text
        let str (s: string) = encodedText s

        /// Specifies HTML text that is injected directly into the output HTML
        let rawText (s: string) = (HtmlString(s) :> IHtmlContent)

        /// Specifies a custom HTML element
        let custom tag attrs content = makeElement tag attrs content

        /// Specifies an HTML element using an arbitrary object in a formatting context.
        ///
        /// If the object is an IHtmlElement or a sequence of IHtmlElement that content is used. Otherwise 
        /// the object will be rendered as HTML using the best available HTML formatter in 
        /// the given context, or else as plain text, and the results treated as encoded text.
        let embed (context: FormatContext) (value: obj) = 
            (HtmlElement.Obj(PocketViewTags.embed(value, context)) :> IHtmlContent)

        /// Specifies an HTML element using an arbitrary object without a formatting context.
        ///
        /// If the object is an IHtmlElement or a sequence of IHtmlElement that content is used. Otherwise 
        /// the object will be rendered as HTML using the best available HTML formatter
        /// with no context, or else as plain text, and the results treated as encoded text.
        let embedNoContext (value: obj) = 
            (HtmlElement.Obj(value) :> IHtmlContent)

    /// Contains functions to specify common HTML attributes.
    [<AutoOpen>]
    module HtmlAttributes =
        /// Specifies an HTML attribute
        let _defaultChecked (b: bool) = HtmlAttribute ("defaultChecked", b)
        /// Specifies an HTML attribute
        let _defaultValue (s: string) = HtmlAttribute ("defaultValue", s)
        /// Specifies an HTML attribute
        let _accept (s: string) = HtmlAttribute ("accept", s)
        /// Specifies an HTML attribute
        let _acceptCharset (s: string) = HtmlAttribute ("acceptCharset", s)
        /// Specifies an HTML attribute
        let _accessKey (s: string) = HtmlAttribute ("accessKey", s)
        /// Specifies an HTML attribute
        let _action (s: string) = HtmlAttribute ("action", s)
        /// Specifies an HTML attribute
        let _allowFullScreen (b: bool) = HtmlAttribute ("allowFullScreen", b)
        /// Specifies an HTML attribute
        let _allowTransparency (b: bool) = HtmlAttribute ("allowTransparency", b)
        /// Specifies an HTML attribute
        let _alt (s: string) = HtmlAttribute ("alt", s)
        /// Specifies an HTML attribute
        let _async (b: bool) = HtmlAttribute ("async", b)
        /// Specifies an HTML attribute
        let _autoComplete (s: string) = HtmlAttribute ("autoComplete", s)
        /// Specifies an HTML attribute
        let _autoFocus (b: bool) = HtmlAttribute ("autoFocus", b)
        /// Specifies an HTML attribute
        let _autoPlay (b: bool) = HtmlAttribute ("autoPlay", b)
        /// Specifies an HTML attribute
        let _capture (b: bool) = HtmlAttribute ("capture", b)
        /// Specifies an HTML attribute
        let _cellPadding (s: string) = HtmlAttribute ("cellPadding", s)
        /// Specifies an HTML attribute
        let _cellSpacing (s: string) = HtmlAttribute ("cellSpacing", s)
        /// Specifies an HTML attribute
        let _charSet (s: string) = HtmlAttribute ("charSet", s)
        /// Specifies an HTML attribute
        let _challenge (s: string) = HtmlAttribute ("challenge", s)
        /// Specifies an HTML attribute
        let _checked (b: bool) = HtmlAttribute ("checked", b)
        /// Specifies an HTML attribute
        let _classID (s: string) = HtmlAttribute ("classID", s)
        /// Specifies an HTML attribute
        let _class (s: string) = HtmlAttribute ("class", s)
        /// Specifies an HTML attribute
        let _cols (v: float) = HtmlAttribute("cols", v)
        /// Specifies an HTML attribute
        let _colSpan (v: float) = HtmlAttribute("colSpan", v)
        /// Specifies an HTML attribute
        let _content (s: string) = HtmlAttribute ("content", s)
        /// Specifies an HTML attribute
        let _contentEditable (b: bool) = HtmlAttribute ("contentEditable", b)
        /// Specifies an HTML attribute
        let _contextMenu (s: string) = HtmlAttribute ("contextMenu", s)
        /// Specifies an HTML attribute
        let _controls (b: bool) = HtmlAttribute ("controls", b)
        /// Specifies an HTML attribute
        let _coords (s: string) = HtmlAttribute ("coords", s)
        /// Specifies an HTML attribute
        let _crossOrigin (s: string) = HtmlAttribute ("crossOrigin", s)
        /// Specifies an HTML attribute
        let _ddata (s: string) = HtmlAttribute ("ddata", s)
        /// Specifies an HTML attribute
        let _dateTime (s: string) = HtmlAttribute ("dateTime", s)
        /// Specifies an HTML attribute
        let _default (b: bool) = HtmlAttribute ("default", b)
        /// Specifies an HTML attribute
        let _defer (b: bool) = HtmlAttribute ("defer", b)
        /// Specifies an HTML attribute
        let _dir (s: string) = HtmlAttribute ("dir", s)
        /// Specifies an HTML attribute
        let _disabled (b: bool) = HtmlAttribute ("disabled", b)
        /// Specifies an HTML attribute
        let _download (s: string) = HtmlAttribute ("download", s)
        /// Specifies an HTML attribute
        let _draggable (b: bool) = HtmlAttribute ("draggable", b)
        /// Specifies an HTML attribute
        let _encType (s: string) = HtmlAttribute ("encType", s)
        /// Specifies an HTML attribute
        let _form (s: string) = HtmlAttribute ("form", s)
        /// Specifies an HTML attribute
        let _formAction (s: string) = HtmlAttribute ("formAction", s)
        /// Specifies an HTML attribute
        let _formEncType (s: string) = HtmlAttribute ("formEncType", s)
        /// Specifies an HTML attribute
        let _formMethod (s: string) = HtmlAttribute ("formMethod", s)
        /// Specifies an HTML attribute
        let _formNoValidate (b: bool) = HtmlAttribute ("formNoValidate", b)
        /// Specifies an HTML attribute
        let _formTarget (s: string) = HtmlAttribute ("formTarget", s)
        /// Specifies an HTML attribute
        let _frameBorder (s: string) = HtmlAttribute ("frameBorder", s)
        /// Specifies an HTML attribute
        let _headers (s: string) = HtmlAttribute ("headers", s)
        /// Specifies an HTML attribute
        let _height (s: string) = HtmlAttribute ("height", s)
        /// Specifies an HTML attribute
        let _hidden (b: bool) = HtmlAttribute ("hidden", b)
        /// Specifies an HTML attribute
        let _high (v: float) = HtmlAttribute("high", v)
        /// Specifies an HTML attribute
        let _href (s: string) = HtmlAttribute ("href", s)
        /// Specifies an HTML attribute
        let _hrefLang (s: string) = HtmlAttribute ("hrefLang", s)
        /// Specifies an HTML attribute
        let _htmlFor (s: string) = HtmlAttribute ("htmlFor", s)
        /// Specifies an HTML attribute
        let _httpEquiv (s: string) = HtmlAttribute ("httpEquiv", s)
        /// Specifies an HTML attribute
        let _icon (s: string) = HtmlAttribute ("icon", s)
        /// Specifies an HTML attribute
        let _id (s: string) = HtmlAttribute ("id", s)
        /// Specifies an HTML attribute
        let _inputMode (s: string) = HtmlAttribute ("inputMode", s)
        /// Specifies an HTML attribute
        let _integrity (s: string) = HtmlAttribute ("integrity", s)
        /// Specifies an HTML attribute
        let _is (s: string) = HtmlAttribute ("is", s)
        /// Specifies an HTML attribute
        let _keyParams (s: string) = HtmlAttribute ("keyParams", s)
        /// Specifies an HTML attribute
        let _keyType (s: string) = HtmlAttribute ("keyType", s)
        /// Specifies an HTML attribute
        let _kind (s: string) = HtmlAttribute ("kind", s)
        /// Specifies an HTML attribute
        let _label (s: string) = HtmlAttribute ("label", s)
        /// Specifies an HTML attribute
        let _lang (s: string) = HtmlAttribute ("lang", s)
        /// Specifies an HTML attribute
        let _language (s: string) = HtmlAttribute ("language", s)
        /// Specifies an HTML attribute
        let _list (s: string) = HtmlAttribute ("list", s)
        /// Specifies an HTML attribute
        let _loop (b: bool) = HtmlAttribute ("loop", b)
        /// Specifies an HTML attribute
        let _low (v: float) = HtmlAttribute("low", v)
        /// Specifies an HTML attribute
        let _manifest (s: string) = HtmlAttribute ("manifest", s)
        /// Specifies an HTML attribute
        let _marginHeight (v: float) = HtmlAttribute("marginHeight", v)
        /// Specifies an HTML attribute
        let _marginWidth (v: float) = HtmlAttribute("marginWidth", v)
        /// Specifies an HTML attribute
        let _max (s: string) = HtmlAttribute ("max", s)
        /// Specifies an HTML attribute
        let _maxLength (v: float) = HtmlAttribute("maxLength", v)
        /// Specifies an HTML attribute
        let _media (s: string) = HtmlAttribute ("media", s)
        /// Specifies an HTML attribute
        let _mediaGroup (s: string) = HtmlAttribute ("mediaGroup", s)
        /// Specifies an HTML attribute
        let _method (s: string) = HtmlAttribute ("method", s)
        /// Specifies an HTML attribute
        let _min (s: string) = HtmlAttribute ("min", s)
        /// Specifies an HTML attribute
        let _minLength (v: float) = HtmlAttribute("minLength", v)
        /// Specifies an HTML attribute
        let _multiple (b: bool) = HtmlAttribute ("multiple", b)
        /// Specifies an HTML attribute
        let _muted (b: bool) = HtmlAttribute ("muted", b)
        /// Specifies an HTML attribute
        let _name (s: string) = HtmlAttribute ("name", s)
        /// Specifies an HTML attribute
        let _noValidate (b: bool) = HtmlAttribute ("noValidate", b)
        /// Specifies an HTML attribute
        let _onMouseOut (s: string) = HtmlAttribute ("onmouseout", s)
        /// Specifies an HTML attribute
        let _onMouseOver (s: string) = HtmlAttribute ("onmouseover", s)
        /// Specifies an HTML attribute
        let _open (b: bool) = HtmlAttribute ("open", b)
        /// Specifies an HTML attribute
        let _optimum (v: float) = HtmlAttribute("optimum", v)
        /// Specifies an HTML attribute
        let _pattern (s: string) = HtmlAttribute ("pattern", s)
        /// Specifies an HTML attribute
        let _placeholder (s: string) = HtmlAttribute ("placeholder", s)
        /// Specifies an HTML attribute
        let _poster (s: string) = HtmlAttribute ("poster", s)
        /// Specifies an HTML attribute
        let _preload (s: string) = HtmlAttribute ("preload", s)
        /// Specifies an HTML attribute
        let _radioGroup (s: string) = HtmlAttribute ("radioGroup", s)
        /// Specifies an HTML attribute
        let _readOnly (b: bool) = HtmlAttribute ("readOnly", b)
        /// Specifies an HTML attribute
        let _rel (s: string) = HtmlAttribute ("rel", s)
        /// Specifies an HTML attribute
        let _required (b: bool) = HtmlAttribute ("required", b)
        /// Specifies an HTML attribute
        let _role (s: string) = HtmlAttribute ("role", s)
        /// Specifies an HTML attribute
        let _rows (v: float) = HtmlAttribute("rows", v)
        /// Specifies an HTML attribute
        let _rowSpan (v: float) = HtmlAttribute("rowSpan", v)
        /// Specifies an HTML attribute
        let _sandbox (s: string) = HtmlAttribute ("sandbox", s)
        /// Specifies an HTML attribute
        let _scope (s: string) = HtmlAttribute ("scope", s)
        /// Specifies an HTML attribute
        let _scoped (b: bool) = HtmlAttribute ("scoped", b)
        /// Specifies an HTML attribute
        let _scrolling (s: string) = HtmlAttribute ("scrolling", s)
        /// Specifies an HTML attribute
        let _seamless (b: bool) = HtmlAttribute ("seamless", b)
        /// Specifies an HTML attribute
        let _selected (b: bool) = HtmlAttribute ("selected", b)
        /// Specifies an HTML attribute
        let _shape (s: string) = HtmlAttribute ("shape", s)
        /// Specifies an HTML attribute
        let _size (v: float) = HtmlAttribute("size", v)
        /// Specifies an HTML attribute
        let _sizes (s: string) = HtmlAttribute ("sizes", s)
        /// Specifies an HTML attribute
        let _span (v: float) = HtmlAttribute("span", v)
        /// Specifies an HTML attribute
        let _spellCheck (b: bool) = HtmlAttribute ("spellCheck", b)
        /// Specifies an HTML attribute
        let _src (s: string) = HtmlAttribute ("src", s)
        /// Specifies an HTML attribute
        let _srcDoc (s: string) = HtmlAttribute ("srcDoc", s)
        /// Specifies an HTML attribute
        let _srcLang (s: string) = HtmlAttribute ("srcLang", s)
        /// Specifies an HTML attribute
        let _srcSet (s: string) = HtmlAttribute ("srcSet", s)
        /// Specifies an HTML attribute
        let _start (v: float) = HtmlAttribute("start", v)
        /// Specifies an HTML attribute
        let _step (s: string) = HtmlAttribute ("step", s)
        /// Specifies an HTML attribute
        let _style (s: string list) = HtmlAttribute ("style", (s |> String.concat " "))
        /// Specifies an HTML attribute
        let _summary (s: string) = HtmlAttribute ("summary", s)
        /// Specifies an HTML attribute
        let _tabIndex (v: float) = HtmlAttribute("tabIndex", v)
        /// Specifies an HTML attribute
        let _target (s: string) = HtmlAttribute ("target", s)
        /// Specifies an HTML attribute
        let _title (s: string) = HtmlAttribute ("title", s)
        /// Specifies an HTML attribute
        let _type (s: string) = HtmlAttribute ("type", s)
        /// Specifies an HTML attribute
        let _useMap (s: string) = HtmlAttribute ("useMap", s)
        /// Specifies an HTML attribute
        let _value (s: string) = HtmlAttribute ("value", s)
        /// Specifies an HTML attribute
        let _width (s: string) = HtmlAttribute ("width", s)
        /// Specifies an HTML attribute
        let _wmode (s: string) = HtmlAttribute ("wmode", s)
        /// Specifies an HTML attribute
        let _wrap (s: string) = HtmlAttribute ("wrap", s)
        /// Specifies an HTML attribute
        let _about (s: string) = HtmlAttribute ("about", s)
        /// Specifies an HTML attribute
        let _datatype (s: string) = HtmlAttribute ("datatype", s)
        /// Specifies an HTML attribute
        let _inlist (s: string) = HtmlAttribute ("inlist", s)
        /// Specifies an HTML attribute
        let _prefix (s: string) = HtmlAttribute ("prefix", s)
        /// Specifies an HTML attribute
        let _property (s: string) = HtmlAttribute ("property", s)
        /// Specifies an HTML attribute
        let _resource (s: string) = HtmlAttribute ("resource", s)
        /// Specifies an HTML attribute
        let _typeof (s: string) = HtmlAttribute ("typeof", s)
        /// Specifies an HTML attribute
        let _vocab (s: string) = HtmlAttribute ("vocab", s)
        /// Specifies an HTML attribute
        let _autoCapitalize (s: string) = HtmlAttribute ("autoCapitalize", s)
        /// Specifies an HTML attribute
        let _autoCorrect (s: string) = HtmlAttribute ("autoCorrect", s)
        /// Specifies an HTML attribute
        let _autoSave (s: string) = HtmlAttribute ("autoSave", s)
        /// Specifies an HTML attribute
        let _color (s: string) = HtmlAttribute ("color", s)
        /// Specifies an HTML attribute
        let _itemProp (s: string) = HtmlAttribute ("itemProp", s)
        /// Specifies an HTML attribute
        let _itemScope (b: bool) = HtmlAttribute ("itemScope", b)
        /// Specifies an HTML attribute
        let _itemType (s: string) = HtmlAttribute ("itemType", s)
        /// Specifies an HTML attribute
        let _itemID (s: string) = HtmlAttribute ("itemID", s)
        /// Specifies an HTML attribute
        let _itemRef (s: string) = HtmlAttribute ("itemRef", s)
        /// Specifies an HTML attribute
        let _results (v: float) = HtmlAttribute("results", v)
        /// Specifies an HTML attribute
        let _security (s: string) = HtmlAttribute ("security", s)
        /// Specifies an HTML attribute
        let _unselectable (b: bool) = HtmlAttribute ("unselectable", b)
        /// Specifies an HTML attribute
        let _custom (k: string, v: obj) = HtmlAttribute (k, v) 
