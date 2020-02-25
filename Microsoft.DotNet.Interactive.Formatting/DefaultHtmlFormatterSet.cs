// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultHtmlFormatterSet : FormatterSetBase
    {
        public DefaultHtmlFormatterSet() : base(DefaultFormatters())
        {
        }

        protected override bool TryInferFormatter(Type type, out ITypeFormatter formatter)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>))
            {
                formatter = Formatter.Create(
                    type,
                    (obj, writer) =>
                    {
                        var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                            (type.GetGenericArguments());

                        var array = toArray.Invoke(null, new[]
                        {
                            obj
                        });

                        writer.Write(array.ToDisplayString(HtmlFormatter.MimeType));
                    },
                    HtmlFormatter.MimeType);
                return true;
            }

            formatter = null;
            return false;
        }

        private static ConcurrentDictionary<Type, ITypeFormatter> DefaultFormatters() =>
            new ConcurrentDictionary<Type, ITypeFormatter>
            {
                [typeof(DateTime)] = new HtmlFormatter<DateTime>((value, writer) => writer.Write(value.ToString("u"))),

                [typeof(DateTimeOffset)] = new HtmlFormatter<DateTimeOffset>((value, writer) => writer.Write(value.ToString("u"))),

                [typeof(ExpandoObject)] = new HtmlFormatter<ExpandoObject>((obj, writer) =>
                {
                    var headers = new List<IHtmlContent>();
                    var values = new List<IHtmlContent>();

                    foreach (var pair in obj.OrderBy(p => p.Key))
                    {
                        headers.Add(th(pair.Key));
                        values.Add(td(pair.Value));
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

                [typeof(HtmlString)] = new HtmlFormatter<HtmlString>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(JsonString)] = new HtmlFormatter<JsonString>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(PocketView)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(ReadOnlyMemory<char>)] = new HtmlFormatter<ReadOnlyMemory<char>>((memory, writer) =>
                {
                    PocketView view = span(memory.Span.ToString());

                    view.WriteTo(writer, HtmlEncoder.Default);
                }),

                [typeof(string)] = new HtmlFormatter<string>((s, writer) => writer.Write(span(s))),

                [typeof(TimeSpan)] = new HtmlFormatter<TimeSpan>((timespan, writer) =>
                {
                    writer.Write(timespan.ToString());
                }),

                [typeof(Type)] = _formatterForSystemType,

                [typeof(Type).GetType()] = _formatterForSystemType,
            };

        private static readonly HtmlFormatter<Type> _formatterForSystemType  = new HtmlFormatter<Type>((type, writer) =>
        {
            PocketView view = span(
                a[href: $"https://docs.microsoft.com/dotnet/api/{type.FullName}?view=netcore-3.0"](
                    type.ToDisplayString()));

            view.WriteTo(writer, HtmlEncoder.Default);
        });
    }
}