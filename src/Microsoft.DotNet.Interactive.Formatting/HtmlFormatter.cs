// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting;

public static class HtmlFormatter
{
    static HtmlFormatter()
    {
        Formatter.Clearing += Initialize;

        void Initialize()
        {
            FormattersForAnyObject = new FormatterMapByType(typeof(HtmlFormatter<>), nameof(HtmlFormatter<object>.CreateTreeViewFormatterForAnyObject));
        }
    }

    // FIX: (HtmlFormatter) this can return a formatter with the wrong MIME type
    public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
        Formatter.GetPreferredFormatterFor(type, MimeType);

    public static ITypeFormatter GetPreferredFormatterFor<T>() =>
        GetPreferredFormatterFor(typeof(T));

    public const string MimeType = "text/html";

    internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type) =>
        FormattersForAnyObject.GetOrCreateFormatterForType(type);

    internal static ITypeFormatter GetDefaultFormatterForAnyEnumerable(Type type) =>
        FormattersForAnyEnumerable.GetOrCreateFormatterForType(type);

    internal static void FormatAndStyleAsPlainText(
        object value,
        FormatContext context,
        string mimeType = PlainTextFormatter.MimeType)
    {
        context.RequireDefaultStyles();
        var tag = TagWithPlainTextStyling(value, mimeType);
        tag.WriteTo(context);
    }

    internal static PocketView TagWithPlainTextStyling(
        object value, 
        string mimeType = PlainTextFormatter.MimeType)
    {
        PocketView tag = div(pre(value.ToDisplayString(mimeType)));
        tag.HtmlAttributes["class"] = "dni-plaintext";
        return tag;
    }

    internal static FormatterMapByType FormattersForAnyObject;

    internal static FormatterMapByType FormattersForAnyEnumerable =
        new(typeof(HtmlFormatter<>), nameof(HtmlFormatter<object>.CreateTableFormatterForAnyEnumerable));

    internal static readonly ITypeFormatter[] DefaultFormatters =
    {
        new HtmlFormatter<DateTime>((dateTime, context) =>
        {
            PocketView view = span(dateTime.ToString("u"));
            view.WriteTo(context);
            return true;
        }),

        new HtmlFormatter<DateTimeOffset>((dateTime, context) =>
        {
            PocketView view = span(dateTime.ToString("u"));
            view.WriteTo(context);
            return true;
        }),

        new HtmlFormatter<ExpandoObject>((value, context) =>
        {
            var headers = new List<IHtmlContent>();
            var values = new List<IHtmlContent>();

            foreach (var pair in value.OrderBy(p => p.Key))
            {
                // Note, embeds the keys and values as arbitrary objects into the HTML content,
                // ultimately rendered by PocketView
                headers.Add(th(pair.Key));
                values.Add(td(pair.Value));
            }

            PocketView view =
                table(
                    thead(
                        tr(
                            headers)),
                    tbody(
                        tr(
                            values)));

            view.WriteTo(context);
            return true;
        }),

        new HtmlFormatter<PocketView>((view, context) =>
        {
            view.WriteTo(context);
            return true;
        }),

        new HtmlFormatter<IHtmlContent>((view, context) =>
        {
            view.WriteTo(context.Writer, HtmlEncoder.Default);
            return true;
        }),

        new HtmlFormatter<ReadOnlyMemory<char>>((memory, context) =>
        {
            PocketView view = span(memory.Span.ToString());

            view.WriteTo(context);
            return true;
        }),

        new HtmlFormatter<string>((s, context) =>
        {
            // If PreformatPlainText is true, then strings
            // will have line breaks and white-space preserved
            FormatAndStyleAsPlainText(s, context);
            return true;
        }),

        new HtmlFormatter<TimeSpan>((timespan, context) =>
        {
            PocketView view = span(timespan.ToString());
            view.WriteTo(context);
            return true;
        }),

        new HtmlFormatter<Type>((type, context) =>
        {
            var text = type.ToDisplayString(PlainTextFormatter.MimeType);

            // This is approximate
            var isKnownDocType =
                type.Namespace is not null &&
                type.FullName is not null &&
                (type.Namespace == "System" ||
                 type.Namespace.StartsWith("System.") ||
                 type.Namespace.StartsWith("Microsoft.")) &&
                !type.IsAnonymous();

            if (!isKnownDocType)
            {
                context.Writer.Write(text.HtmlEncode());
            }
            else
            {
                //system.collections.generic.list-1
                //system.collections.generic.list-1.enumerator
                Type genericTypeDefinition;
                if (type.IsGenericType)
                {
                    genericTypeDefinition = type.GetGenericTypeDefinition();
                }
                else
                {
                    genericTypeDefinition = type;
                }

                var typeLookupName =
                    genericTypeDefinition.FullName.ToLower().Replace("+", ".").Replace("`", "-");

                PocketView view =
                    span(a[href: $"https://docs.microsoft.com/dotnet/api/{typeLookupName}?view=net-7.0"](
                        text));
                view.WriteTo(context);
            }

            return true;
        }),

        // Transform ReadOnlyMemory to an array for formatting
        new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
            mimeType: MimeType,
            format: (value, context) =>
            {
                var actualType = value.GetType();
                var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                    (actualType.GetGenericArguments());

                var array = toArray.Invoke(null, new[] { value });

                array.FormatTo(context, MimeType);

                return true;
            }),

        new HtmlFormatter<Enum>((enumValue, context) =>
        {
            PocketView view = span(enumValue.ToString());
            view.WriteTo(context);
            return true;
        }),
        
        // BigInteger should be displayed as plain text
        new HtmlFormatter<BigInteger>((value, context) =>
        {
            FormatAndStyleAsPlainText(value, context);
            return true;
        }),

         new HtmlFormatter<decimal>((value, context) =>
         {
             FormatAndStyleAsPlainText(value, context);
             return true;
         }),

        new HtmlFormatter<object>((value, context) =>
        {
            context.RequireDefaultStyles();

            var type = value.GetType();
            var formatter = GetDefaultFormatterForAnyObject(type);
            return formatter.Format(value, context);
        }),

        // Final last resort is to convert to plain text
        new HtmlFormatter<object>((value, context) =>
        {
            if (value is null)
            {
                FormatAndStyleAsPlainText(Formatter.NullString, context);
            }
            else
            {
                FormatAndStyleAsPlainText(value, context);
            }

            return true;
        }),

        new HtmlFormatter<JsonDocument>((doc, context) =>
        {
            doc.RootElement.FormatTo(context, MimeType);
            return true;
        }),

        new HtmlFormatter<JsonElement>((element, context) =>
        {
            PocketView view = null;

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:

                    var keysAndValues = element.EnumerateObject().ToArray();

                    view = details[@class: "dni-treeview"](
                        summary(
                            span[@class: "dni-code-hint"](code(element.ToString()))),
                        div(
                            Html.Table(
                                headers: null,
                                rows: keysAndValues.Select(
                                    a => (IHtmlContent)
                                        tr(
                                            td(a.Name), td(a.Value))).ToArray())));

                    break;

                case JsonValueKind.Array:

                    var arrayEnumerator = element.EnumerateArray().ToArray();

                    view = details[@class: "dni-treeview"](
                        summary(
                            span[@class: "dni-code-hint"](code(element.ToString()))),
                        div(
                            Html.Table(
                                headers: null,
                                rows: arrayEnumerator.Select(
                                    a => (IHtmlContent) tr(td(a))).ToArray())));

                    break;

                case JsonValueKind.String:
                    var value = element.GetString();
                    view = span($"\"{value}\"");

                    break;

                case JsonValueKind.Number:
                    view = span(element.GetSingle());
                    break;

                case JsonValueKind.True:
                    view = span("true");
                    break;

                case JsonValueKind.False:
                    view = span("false");
                    break;

                case JsonValueKind.Null:
                    view = span(Formatter.NullString);
                    break;

                default:
                    return false;
            }

            context.RequireDefaultStyles();

            view.WriteTo(context);

            return true;
        })
    };

    private static readonly Lazy<IHtmlContent> _defaultStyles = new(() => style(new HtmlString(@"
.dni-code-hint {
    font-style: italic;
    overflow: hidden;
    white-space: nowrap;
}
.dni-treeview {
    white-space: nowrap;
}
.dni-treeview td {
    vertical-align: top;
    text-align: start;
}
details.dni-treeview {
    padding-left: 1em;
}
table td {
    text-align: start;
}
table tr { 
    vertical-align: top; 
    margin: 0em 0px;
}
table tr td pre 
{ 
    vertical-align: top !important; 
    margin: 0em 0px !important;
} 
table th {
    text-align: start;
}
")));

    internal static IHtmlContent DefaultStyles() => _defaultStyles.Value;

    public static void RequireDefaultStyles(this FormatContext context)
    {
        context.RequireOnComplete("dni-styles", DefaultStyles());
    }
}