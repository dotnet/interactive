// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultHtmlFormatterSet
    {
        static internal readonly ITypeFormatter[] DefaultFormatters =
            new ITypeFormatter[]
            {
                new HtmlFormatter<DateTime>((context, dateTime, writer) =>
                {
                    PocketView view = span(dateTime.ToString("u"));
                    view.WriteTo(context, writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<DateTimeOffset>((context, dateTime, writer) =>
                {
                    PocketView view = span(dateTime.ToString("u"));
                    view.WriteTo(context, writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<ExpandoObject>((context, obj, writer) =>
                {
                    var headers = new List<IHtmlContent>();
                    var values = new List<IHtmlContent>();

                    foreach (var pair in obj.OrderBy(p => p.Key))
                    {
                        // Note, embeds the keys and values as arbitrary objects into the HTML content,
                        // ultimately rendered by PocketView
                        headers.Add(th(arbitrary(pair.Key)));
                        values.Add(td(arbitrary(pair.Value)));
                    }

                    PocketView view =
                      table(
                        thead(
                            tr(
                                headers)),
                        tbody(
                            tr(
                                values)));

                    var innerContext = context.NestedInTable();
                    view.WriteTo(innerContext, writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<PocketView>((context, view, writer) =>
                {
                    view.WriteTo(context, writer, HtmlEncoder.Default);
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

                    view.WriteTo(context, writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<string>((context, s, writer) =>
                {
                    writer.Write(s.HtmlEncode());
                    return true;
                }),

                new HtmlFormatter<TimeSpan>((context, timespan, writer) =>
                {
                    PocketView view = span(timespan.ToString());
                    view.WriteTo(context, writer, HtmlEncoder.Default);
                    return true;
                }),

                new HtmlFormatter<Type>((context, type, writer) =>
                {
                    PocketView view = span(
                        a[href: $"https://docs.microsoft.com/dotnet/api/{type.FullName}?view=netcore-3.0"](
                            type.ToDisplayString(PlainTextFormatter.MimeType)));

                    view.WriteTo(context, writer, HtmlEncoder.Default);
                    return true;
                }),

                // Transform ReadOnlyMemory to an array for formatting
                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: HtmlFormatter.MimeType,
                    format: (context, obj, writer) =>
                        {
                            var actualType = obj.GetType();
                            var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                                (actualType.GetGenericArguments());

                            var array = toArray.Invoke(null, new[] { obj });

                            array.FormatTo(context, writer, HtmlFormatter.MimeType);
                            return true;
                        }),

                new HtmlFormatter<Enum>((context, enumValue, writer) =>
                {
                    PocketView view = span(enumValue.ToString());
                    view.WriteTo(context, writer, HtmlEncoder.Default);
                    return true;
                }),

                // Try to display enumerable results as tables. This will return false for nested tables.
                new HtmlFormatter<IEnumerable>((context, obj, writer) =>
                {
                    var type = obj.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    return formatter.Format(context, obj, writer);
                }),

                // Try to display object results as tables. This will return false for nested tables.
                new HtmlFormatter<object>((context, obj, writer) =>
                {
                    var type = obj.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyObject(type);
                    return formatter.Format(context, obj, writer);
                }),
                
                // Final last resort is to convert to plain text and embed pre-formatted
                new HtmlFormatter<object>((context, obj, writer) =>
                {
                    var html = HtmlFormatter.DisplayEmbeddedObjectAsPlainText(context, obj);
                    html.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                })

            };            
    }
}