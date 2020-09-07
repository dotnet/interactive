// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;
using System;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class PocketViewTags
    {
        public static dynamic _ { get; } = new PocketView();
        public static dynamic a => _.a;
        public static dynamic area => _.area;
        public static dynamic aside => _.aside;
        public static dynamic b => _.b;
        public static dynamic body => _.body;
        public static dynamic br => _.br;
        public static dynamic button => _.button;
        public static dynamic caption => _.caption;
        public static dynamic center => _.center;
        public static dynamic circle => _.circle;
        public static dynamic code => _.code;
        public static dynamic colgroup => _.colgroup;
        public static dynamic dd => _.dd;
        public static dynamic details => _.details;
        public static dynamic div => _.div;
        public static dynamic dl => _.dl;
        public static dynamic dt => _.dt;
        public static dynamic em => _.em;
        public static dynamic figure => _.figure;
        public static dynamic font => _.font;
        public static dynamic form => _.form;
        public static dynamic g => _.g;
        public static dynamic h1 => _.h1;
        public static dynamic h2 => _.h2;
        public static dynamic h3 => _.h3;
        public static dynamic h4 => _.h4;
        public static dynamic h5 => _.h5;
        public static dynamic h6 => _.h6;
        public static dynamic head => _.head;
        public static dynamic header => _.header;
        public static dynamic hgroup => _.hgroup;
        public static dynamic hr => _.hr;
        public static dynamic html => _.html;
        public static dynamic i => _.i;
        public static dynamic iframe => _.iframe;
        public static dynamic img => _.img;
        public static dynamic input => _.input;
        public static dynamic label => _.label;
        public static dynamic li => _.li;
        public static dynamic line => _.line;
        public static dynamic link => _.link;
        public static dynamic main => _.main;
        public static dynamic menu => _.menu;
        public static dynamic menuitem => _.menuitem;
        public static dynamic meta => _.meta;
        public static dynamic meter => _.meter;
        public static dynamic nav => _.nav;
        public static dynamic ol => _.ol;
        public static dynamic optgroup => _.optgroup;
        public static dynamic option => _.option;
        public static dynamic p => _.p;
        public static dynamic pre => _.pre;
        public static dynamic progress => _.progress;
        public static dynamic q => _.q;
        public static dynamic script => _.script;
        public static dynamic section => _.section;
        public static dynamic select => _.select;
        public static dynamic small => _.small;
        public static dynamic source => _.source;
        public static dynamic span => _.span;
        public static dynamic strike => _.strike;
        public static dynamic style => _.style;
        public static dynamic strong => _.strong;
        public static dynamic sub => _.sub;
        public static dynamic sup => _.sup;
        public static dynamic svg => _.svg;
        public static dynamic table => _.table;
        public static dynamic tbody => _.tbody;
        public static dynamic td => _.td;
        public static dynamic text => _.text;
        public static dynamic textarea => _.textarea;
        public static dynamic tfoot => _.tfoot;
        public static dynamic th => _.th;
        public static dynamic thead => _.thead;
        public static dynamic title => _.title;
        public static dynamic tr => _.tr;
        public static dynamic u => _.u;
        public static dynamic ul => _.ul;
        public static dynamic video => _.video;

        /// <summary>An element that consists of an HtmlString</summary>
        public static object str(string text)
        {
            return new HtmlString(text);
        }
        /// <summary>Create an object suitable for delayed expansion to HTML</summary>
        public static object embed(object obj, FormatContext context)
        {
            return new HtmlFormatter.EmbeddedFormat(context, obj);
        }

    }
}