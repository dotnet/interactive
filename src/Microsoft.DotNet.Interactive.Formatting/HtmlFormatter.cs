// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class HtmlFormatter
    {
        static HtmlFormatter()
        {
            Formatter.Clearing += Initialize;

            void Initialize() => MaxProperties = DefaultMaxProperties;
        }

        /// <summary>
        ///   Indicates the maximum number of properties to show in the default HTML display of arbitrary objects.
        ///   If set to zero no properties are shown.
        /// </summary>
        public static int MaxProperties { get; set; } = DefaultMaxProperties;

        internal const int DefaultMaxProperties = 20;

        public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
            Formatter.GetPreferredFormatterFor(type, MimeType);

        public static ITypeFormatter GetPreferredFormatterFor<T>() =>
            GetPreferredFormatterFor(typeof(T));

        public const string MimeType = "text/html";

        internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type, bool includeInternals = false) =>
            FormattersForAnyObject.GetOrCreateFormatterForType(type, includeInternals);

        internal static ITypeFormatter GetDefaultFormatterForAnyEnumerable(Type type) =>
            FormattersForAnyEnumerable.GetOrCreateFormatterForType(type, false);

        internal static void FormatAndStyleAsPlainText(
            object text, 
            FormatContext context)
        {
            PocketView tag = div(text.ToDisplayString(PlainTextFormatter.MimeType));
            tag.HtmlAttributes["class"] = "dni-plaintext";
            tag.WriteTo(context);
        }

        internal static FormatterMapByType FormattersForAnyObject =
            new(typeof(HtmlFormatter<>), nameof(HtmlFormatter<object>.CreateForAnyObject));

        internal static FormatterMapByType FormattersForAnyEnumerable =
            new(typeof(HtmlFormatter<>), nameof(HtmlFormatter<object>.CreateForAnyEnumerable));
    }
}