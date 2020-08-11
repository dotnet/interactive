// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultPlainTextFormatterSet
    {
        static internal ITypeFormatter[] DefaultFormatters =
            new ITypeFormatter[]
            {
                new PlainTextFormatter<ExpandoObject>((context, expando, writer) =>
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
                            pair.Value.FormatTo(context, writer);

                            if (i < length - 1)
                            {
                                singleLineFormatter.WritePropertyDelimiter(writer);
                            }
                        }

                        singleLineFormatter.WriteEndObject(writer);
                        return true;
                    }),

                new PlainTextFormatter<IHtmlContent>((context, view, writer) =>
                {
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new PlainTextFormatter<KeyValuePair<string, object>>((context, pair, writer) =>
                {
                    var singleLineFormatter = new SingleLinePlainTextFormatter();
                    writer.Write(pair.Key);
                    singleLineFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(context, writer);
                    return true;
                }),

                new PlainTextFormatter<ReadOnlyMemory<char>>((context, memory, writer) => 
                { 
                    writer.Write(memory.Span.ToString()); 
                    return true;
                }),

                new PlainTextFormatter<TimeSpan>((context, timespan, writer) => 
                { 
                    writer.Write(timespan.ToString()); 
                    return true;
                }),

                new PlainTextFormatter<Type>((context, type, writer) =>
                {
                    if (type.IsAnonymous())
                    {
                        writer.Write("(anonymous)");
                        return true;
                    }

                    type.WriteCSharpDeclarationTo(writer);
                    return true;
                }),

                new PlainTextFormatter<DateTime>((context, value, writer) =>
                {
                    writer.Write(value.ToString("u"));
                    return true;
                }),

                new PlainTextFormatter<DateTimeOffset>((context, value, writer) =>
                {
                    writer.Write(value.ToString("u"));
                    return true;
                }),


                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: PlainTextFormatter.MimeType,
                    format: (context, obj, writer) =>
                    {
                        var actualType = obj.GetType();
                        var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                            (actualType.GetGenericArguments());

                        var array = toArray.Invoke(null, new[] { obj });

                        writer.Write(array.ToDisplayString(PlainTextFormatter.MimeType));
                        return true;
                    }),


                new PlainTextFormatter<TextSpan>((context, span, writer) =>
                    {
                        writer.Write(span.ToString(OutputMode.Ansi));
                        return true;
                    }),

                // Newtonsoft.Json types -- these implement IEnumerable and their default output is not useful, so use their default ToString
                new PlainTextFormatter<Newtonsoft.Json.Linq.JArray>((context, obj, writer) =>
                    {
                        writer.Write(obj);
                        return true;
                    }),

                new PlainTextFormatter<Newtonsoft.Json.Linq.JObject>((context, obj, writer) =>
                    {
                        writer.Write(obj);
                        return true;
                    }),

                // Fallback for IEnumerable
                new PlainTextFormatter<IEnumerable>((context, obj, writer) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString);
                        return true;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    return formatter.Format(context, obj, writer);
                }),

                // Fallback for any object
                new PlainTextFormatter<object>((context, obj, writer) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString);
                        return true;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.GetDefaultFormatterForAnyObject(type);
                    return formatter.Format(context, obj, writer);
                })
            };
    }
}