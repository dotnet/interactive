// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
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
                new HtmlFormatter<DateTime>((dateTime, writer) =>
                {
                    PocketView view = span(dateTime.ToString("u"));
                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                new HtmlFormatter<DateTimeOffset>((dateTime, writer) =>
                {
                    PocketView view = span(dateTime.ToString("u"));
                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                new HtmlFormatter<ExpandoObject>((obj, writer) =>
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

                    IHtmlContent view = table(
                        thead(
                            tr(
                                headers)),
                        tbody(
                            tr(
                                values)));

                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                new HtmlFormatter<IHtmlContent>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                new HtmlFormatter<ReadOnlyMemory<char>>((memory, writer) =>
                {
                    PocketView view = span(memory.Span.ToString());

                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                new HtmlFormatter<string>((s, writer) => writer.Write(s.HtmlEncode())),

                new HtmlFormatter<TimeSpan>((timespan, writer) =>
                {
                    PocketView view = span(timespan.ToString());
                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                new HtmlFormatter<Type>((type, writer) =>
                {
                    PocketView view = span(
                        a[href: $"https://docs.microsoft.com/dotnet/api/{type.FullName}?view=netcore-3.0"](
                            type.ToDisplayString(PlainTextFormatter.MimeType)));

                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                // Transform ReadOnlyMemory to an array for formatting
                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: HtmlFormatter.MimeType,
                    format: (obj, writer) =>
                        {
                            var actualType = obj.GetType();
                            var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                                (actualType.GetGenericArguments());

                            var array = toArray.Invoke(null, new[] { obj });

                            writer.Write(array.ToDisplayString(HtmlFormatter.MimeType));
                        }),

                new HtmlFormatter<Enum>((enumValue, writer) =>
                {
                    PocketView view = span(enumValue.ToString());
                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                new HtmlFormatter<IEnumerable>((obj, writer) =>
                {
                    var type = obj.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    formatter.Format(obj, writer);

                }),

                new HtmlFormatter<object>((obj, writer) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString.HtmlEncode());
                        return;
                    }
                    var type = obj.GetType();
                    var formatter = HtmlFormatter.GetDefaultFormatterForAnyObject(type);
                    formatter.Format(obj, writer);
                })

            };            
    }
}