// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class HtmlFormatter
    {
        public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
            Formatter.GetPreferredFormatterFor(type, MimeType);

        public static ITypeFormatter GetPreferredFormatterFor<T>() =>
            GetPreferredFormatterFor(typeof(T));

        public const string MimeType = "text/html";

        internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type, bool includeInternals = false) =>
            FormattersForAnyObject.GetFormatter(type, includeInternals);

        internal static ITypeFormatter GetDefaultFormatterForAnyEnumerable(Type type) =>
            FormattersForAnyEnumerable.GetFormatter(type, false);

        public static bool PreformatEmbeddedPlainText { get; set; } = false;

        public static bool LeftJustifyEmbeddedPlainText { get; set; } = false;

        static HtmlFormatter()
        {
            Formatter.Clearing += (obj, sender) =>
            {
                PreformatEmbeddedPlainText = false;
                LeftJustifyEmbeddedPlainText = false;
            };
        }

        internal static IHtmlContent DisplayEmbeddedObjectAsPlainText(FormatContext context, object value)
        {
            using var writer = Formatter.CreateWriter();
            Formatter.FormatTo(value, context, writer, PlainTextFormatter.MimeType);
            var text = writer.ToString();
            var html = text.HtmlEncode();

            if (PreformatEmbeddedPlainText && !value.GetType().IsPrimitive)
            {
                var div = new Tag("div");
                if (LeftJustifyEmbeddedPlainText)
                    div.HtmlAttributes["style"] = "white-space: pre; text-align: left;";
                else
                    div.HtmlAttributes["style"] = "white-space: pre;";
                html = div.Containing(html);
            }
            return html;

        }

        internal static PocketView Table(
            List<IHtmlContent> headers,
            List<IHtmlContent> rows) =>
            table(
                thead(
                    tr(
                        headers ?? new List<IHtmlContent>())),
                tbody(
                    rows));

        internal class EmbeddedFormat
        {
            internal FormatContext Context { get; }
            internal object Object { get; }
            internal EmbeddedFormat(FormatContext context, object instance)
                { Object = instance;  Context = context;  }
        }


        internal static ITypeFormatter[] DefaultFormatters { get; } = DefaultHtmlFormatterSet.DefaultFormatters;

        internal static FormatterTable FormattersForAnyObject =
            new FormatterTable(typeof(HtmlFormatter<>), nameof(HtmlFormatter<object>.CreateForAnyObject));

        internal static FormatterTable FormattersForAnyEnumerable =
            new FormatterTable(typeof(HtmlFormatter<>), nameof(HtmlFormatter<object>.CreateForAnyEnumerable));


    }
}