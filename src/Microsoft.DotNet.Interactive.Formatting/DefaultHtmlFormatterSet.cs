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

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultHtmlFormatterSet
    {
        internal static readonly ITypeFormatter[] DefaultFormatters =
            {
                new HtmlFormatter<DateTime>((context, dateTime, writer) =>
                {
                    PocketView view = span(dateTime.ToString("u"));
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<DateTimeOffset>((context, dateTime, writer) =>
                {
                    PocketView view = span(dateTime.ToString("u"));
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<ExpandoObject>((context, value, writer) =>
                {
                    var headers = new List<IHtmlContent>();
                    var values = new List<IHtmlContent>();

                    var innerContext = context.ReduceContent(FormatContext.NestedInTable);
                    foreach (var pair in value.OrderBy(p => p.Key))
                    {
                        // Note, embeds the keys and values as arbitrary objects into the HTML content,
                        // ultimately rendered by PocketView
                        headers.Add(th(str(pair.Key)));
                        values.Add(td(embed(pair.Value, innerContext)));
                    }

                    PocketView view =
                      table(
                        thead(
                            tr(
                                headers)),
                        tbody(
                            tr(
                                values)));

                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<PocketView>((context, view, writer) =>
                {
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<IHtmlContent>((context, view, writer) =>
                {
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<ReadOnlyMemory<char>>((context, memory, writer) =>
                {
                    PocketView view = span(memory.Span.ToString());

                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<string>((context, s, writer) =>
                {
                    // If PlainTextPreformat is true, then strings
                    // will have line breaks and white-space preserved
                    HtmlFormatter.FormatStringAsPlainText(s, writer);
                    return true;
                }),

                new HtmlFormatter<TimeSpan>((context, timespan, writer) =>
                {
                    PocketView view = span(timespan.ToString());
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<Type>((context, type, writer) =>
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
                        writer.Write(text.HtmlEncode());
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
                        view.WriteTo(writer, HtmlEncoder.Default);
                    }

                    return true;
                }),

                // Transform ReadOnlyMemory to an array for formatting
                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: HtmlFormatter.MimeType,
                    format: (context, value, writer) =>
                        {
                            var actualType = value.GetType();
                            var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                                (actualType.GetGenericArguments());

                            var array = toArray.Invoke(null, new[] { value });

                            array.FormatTo(context, writer, HtmlFormatter.MimeType);
                            return true;
                        }),

                new HtmlFormatter<Enum>((context, enumValue, writer) =>
                {
                    PocketView view = span(enumValue.ToString());
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                // Try to display enumerable results as tables. This will return false for nested tables.
                new HtmlFormatter<IEnumerable>((context, value, writer) =>
                {
                    var type = value.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    return formatter.Format(context, value, writer);
                }),

                // BigInteger should be displayed as plain text
                new HtmlFormatter<BigInteger>((context, value, writer) =>
                {
                    HtmlFormatter.FormatObjectAsPlainText(context, value, writer);
                    return true;
                }),

                // Try to display object results as tables. This will return false for nested tables.
                new HtmlFormatter<object>((context, value, writer) =>
                {
                    var type = value.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyObject(type);
                    return formatter.Format(context, value, writer);
                }),
                
                // Final last resort is to convert to plain text
                new HtmlFormatter<object>((context, value, writer) =>
                {
                    if (value is null)
                    {
                        HtmlFormatter.FormatStringAsPlainText(Formatter.NullString, writer);
                        return true;
                    }

                    HtmlFormatter.FormatObjectAsPlainText(context, value, writer);
                    return true;
                }),

                new HtmlFormatter<JsonProperty>((context, property, writer) =>
                {
                    PocketView view =
                        details(
                            summary(property.Name + ":"),
                            property.Value);
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<JsonElement>((context, element, writer) =>
                {
                    IHtmlContent view = null;

                    switch (element.ValueKind)
                    {
                        case JsonValueKind.Object:

                            var keysAndValues = element.EnumerateObject().ToArray();

                            view = details(
                                summary(
                                    span("{&nbsp;&nbsp;}&nbsp;".ToHtmlContent()), i($"{keysAndValues.Length} keys")),
                                div[style: "margin-left:1.2em"](
                                    Html.Table(
                                        headers: null,
                                        rows: keysAndValues.Select(
                                            a => (IHtmlContent)
                                                tr(
                                                    td[style: "word-wrap: break-word; max-width: 150px;"](a.Name), td(a.Value))).ToArray())));

                            break;

                        case JsonValueKind.Array:

                            var arrayEnumerator = element.EnumerateArray().ToArray();

                            view = details(
                                summary(
                                    span("[&nbsp;&nbsp;]&nbsp;".ToHtmlContent()), i($"{arrayEnumerator.Length} items")),
                                div[style: "margin-left:1.2em"](
                                    Html.Table(
                                        headers: null,
                                        rows: arrayEnumerator.Select(
                                            a => (IHtmlContent) tr(td[style: "word-wrap: break-word; max-width: 150px;"](a))).ToArray())));

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

                    view.WriteTo(writer, HtmlEncoder.Default);

                    return true;
                })
        };
    }
}