// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;
using System.Numerics;
using static Microsoft.DotNet.Interactive.Formatting.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultHtmlFormatterSet
    {
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
                    // If PlainTextPreformat is true, then strings
                    // will have line breaks and white-space preserved
                    HtmlFormatter.FormatStringAsPlainText(s, context);
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
                      (type.Namespace == "System" ||
                       type.Namespace.StartsWith("System.") ||
                       type.Namespace.StartsWith("Microsoft."));

                    if (type.IsAnonymous() || !isKnownDocType)
                    {
                        context.Writer.Write(text.HtmlEncode());
                    }
                    else
                    {
                        //system.collections.generic.list-1
                        //system.collections.generic.list-1.enumerator
                        var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

                        var typeLookupName =
                            genericTypeDefinition.FullName.ToLower().Replace("+",".").Replace("`","-");

                        PocketView view = 
                           span(a[href: $"https://docs.microsoft.com/dotnet/api/{typeLookupName}?view=net-5.0"](
                                   text));
                        view.WriteTo(context);
                    }

                    return true;
                }),

                // Transform ReadOnlyMemory to an array for formatting
                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: HtmlFormatter.MimeType,
                    format: (value, context) =>
                    {
                        var actualType = value.GetType();
                        var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                            (actualType.GetGenericArguments());

                        var array = toArray.Invoke(null, new[] { value });

                        array.FormatTo(context, HtmlFormatter.MimeType);

                        return true;
                    }),

                new HtmlFormatter<Enum>((enumValue, context) =>
                {
                    PocketView view = span(enumValue.ToString());
                    view.WriteTo(context);
                    return true;
                }),

                // Try to display enumerable results as tables. This will return false for nested tables.
                new HtmlFormatter<IEnumerable>((value, context) =>
                {
                    var type = value.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    return formatter.Format(value, context);
                }),

                // BigInteger should be displayed as plain text
                new HtmlFormatter<BigInteger>((value, context) =>
                {
                    HtmlFormatter.FormatObjectAsPlainText(value, context);
                    return true;
                }),

                // Try to display object results as tables. This will return false for nested tables.
                new HtmlFormatter<object>((value, context) =>
                {
                    var type = value.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyObject(type);
                    return formatter.Format(value, context);
                }),

                // Final last resort is to convert to plain text
                new HtmlFormatter<object>((value, context) =>
                {
                    if (value is null)
                    {
                        HtmlFormatter.FormatStringAsPlainText(Formatter.NullString, context);
                    }
                    else
                    {
                        HtmlFormatter.FormatObjectAsPlainText(value, context);
                    }

                    return true;
                }),

                new HtmlFormatter<JsonDocument>((doc, context) =>
                {
                    doc.RootElement.FormatTo(context, HtmlFormatter.MimeType);
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
                                    Table(
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
                                    Table(
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

                    var styleElementId = "dni-styles-JsonElement";
                    PocketView css = style[id: styleElementId](new HtmlString(@"    
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
}

details.dni-treeview {
    padding-left: 1em;
}"));
                    context.Require(styleElementId, css);

                    view.WriteTo(context);

                    return true;
                })
        };
    }
}