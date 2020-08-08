// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultPlainTextFormatterSet
    {
        static internal readonly ITypeFormatter[] DefaultFormatters =
            new ITypeFormatter[]
            {
                new PlainTextFormatter<ExpandoObject>((expando, writer) =>
                    {
                        var singleLineFormatter = new SingleLinePlainTextFormatter();
                        singleLineFormatter.WriteStartObject(writer);
                        var pairs = expando.ToArray();
                        var length = pairs.Length;
                        for (var i = 0; i < length; i++)
                        {
                            var pair = pairs[i];
                            writer.Write(pair.Key);
                            singleLineFormatter.WriteNameValueDelimiter(writer);
                            pair.Value.FormatTo(writer);

                            if (i < length - 1)
                            {
                                singleLineFormatter.WritePropertyDelimiter(writer);
                            }
                        }

                        singleLineFormatter.WriteEndObject(writer);
                    }),

                new PlainTextFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                new PlainTextFormatter<KeyValuePair<string, object>>((pair, writer) =>
                {
                    var singleLineFormatter = new SingleLinePlainTextFormatter();
                    writer.Write(pair.Key);
                    singleLineFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(writer);
                }),

                new PlainTextFormatter<ReadOnlyMemory<char>>((memory, writer) => { writer.Write(memory.Span.ToString()); }),

                new PlainTextFormatter<TimeSpan>((timespan, writer) => { writer.Write(timespan.ToString()); }),

                new PlainTextFormatter<Type>((type, writer) =>
                {
                    if (type.IsAnonymous())
                    {
                        writer.Write("(anonymous)");
                        return;
                    }

                    type.WriteCSharpDeclarationTo(writer);
                }),

                new PlainTextFormatter<DateTime>((value, writer) => writer.Write(value.ToString("u"))),

                new PlainTextFormatter<DateTimeOffset>((value, writer) => writer.Write(value.ToString("u"))),

                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: PlainTextFormatter.MimeType,
                    format: (obj, writer) =>
                    {
                        var actualType = obj.GetType();
                        var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                            (actualType.GetGenericArguments());

                        var array = toArray.Invoke(null, new[] { obj });

                        writer.Write(array.ToDisplayString());
                    }),

                new PlainTextFormatter<TextSpan>((span, writer) => writer.Write(span.ToString(OutputMode.Ansi))),

                // Newtonsoft.Json types -- these implement IEnumerable and their default output is not useful, so use their default ToString
                new PlainTextFormatter<Newtonsoft.Json.Linq.JArray>((obj, writer) => writer.Write(obj)),
                new PlainTextFormatter<Newtonsoft.Json.Linq.JObject>((obj, writer) => writer.Write(obj)),

                // Fallback for IEnumerable
                new PlainTextFormatter<IEnumerable>((obj, writer) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString.HtmlEncode());
                        return;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.FormattersForAnyEnumerable.GetFormatter(type, false);
                    formatter.Format(obj, writer);
                }),

                // Fallback for any object
                new PlainTextFormatter<object>((obj, writer) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString.HtmlEncode());
                        return;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.FormattersForAnyObject.GetFormatter(type, false);
                    formatter.Format(obj, writer);
                })
            };
    }
}