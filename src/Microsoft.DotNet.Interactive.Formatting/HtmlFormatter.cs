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
        static HtmlFormatter()
        {
            Formatter.Clearing += (obj, sender) =>
            {
                MaxProperties = DefaultMaxProperties;
                PlainTextPreformat = DefaultPlainTextPreformat;
                PlainTextNoLeftJustify = DefaultPlainTextPreformatNoLeftJustify;
                PlainTextDefaultFont = DefaultPlainTextPreformatDefaultFont;
            };
        }

        /// <summary>
        ///   Indicates the maximum number of properties to show in the default plaintext display of arbitrary objects.
        ///   If set to zero no properties are shown.
        /// </summary>
        public static int MaxProperties { get; set; } = DefaultMaxProperties;

        /// <summary>
        ///   Indicates that any objects unknown to HTML and formatted
        ///   using plain text formatting should be displayed using left-jsutified formatting
        ///   that respects whitespace and newlines in the resulting strings.
        /// </summary>
        public static bool PlainTextPreformat { get; set; } = DefaultPlainTextPreformat;

        /// <summary>
        ///   Indicates that any preformatted plaintext sections should use the default
        ///   font rather than &lt;pre&gt; sections.
        /// </summary>
        public static bool PlainTextDefaultFont { get; set; } = DefaultPlainTextPreformatDefaultFont;

        /// <summary>
        ///   Indicates that any preformatted plaintext sections should not use left justification.
        /// </summary>
        public static bool PlainTextNoLeftJustify { get; set; } = DefaultPlainTextPreformatNoLeftJustify;

        internal const int DefaultMaxProperties = 20;

        internal const bool DefaultPlainTextPreformat = false;

        internal const bool DefaultPlainTextPreformatDefaultFont = false;

        internal const bool DefaultPlainTextPreformatNoLeftJustify = false;

        public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
            Formatter.GetPreferredFormatterFor(type, MimeType);

        public static ITypeFormatter GetPreferredFormatterFor<T>() =>
            GetPreferredFormatterFor(typeof(T));

        public const string MimeType = "text/html";

        internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type, bool includeInternals = false) =>
            FormattersForAnyObject.GetFormatter(type, includeInternals);

        internal static ITypeFormatter GetDefaultFormatterForAnyEnumerable(Type type) =>
            FormattersForAnyEnumerable.GetFormatter(type, false);

        internal static IHtmlContent FormatObjectAsPlainText(FormatContext context, object value)
        {
            using var writer = Formatter.CreateWriter();
            Formatter.FormatTo(value, context, writer, PlainTextFormatter.MimeType);
            var text = writer.ToString();
            return FormatString(text);
        }

        internal static IHtmlContent FormatString(string text)
        {
            var html = text.HtmlEncode();

            if (PlainTextPreformat)
            {
                var div = PlainTextDefaultFont ? new Tag("div") : new Tag("pre");
                if (PlainTextDefaultFont && !PlainTextNoLeftJustify)
                    div.HtmlAttributes["style"] = "white-space: pre; text-align: left;";
                else if (!PlainTextNoLeftJustify)
                    div.HtmlAttributes["style"] = "text-align: left;";
                else if (PlainTextDefaultFont)
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