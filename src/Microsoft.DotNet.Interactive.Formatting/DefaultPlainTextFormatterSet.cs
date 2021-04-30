// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultPlainTextFormatterSet
    {
        internal static ITypeFormatter[] DefaultFormatters =
            {
                new PlainTextFormatter<ExpandoObject>((expando, writer, context) =>
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
                            pair.Value.FormatTo(writer, context);

                            if (i < length - 1)
                            {
                                singleLineFormatter.WritePropertyDelimiter(writer);
                            }
                        }

                        singleLineFormatter.WriteEndObject(writer);
                        return true;
                    }),

                new PlainTextFormatter<IHtmlContent>((view, writer, context) =>
                {
                    view.WriteTo(writer, HtmlEncoder.Default);
                    return true;
                }),

                new PlainTextFormatter<KeyValuePair<string, object>>((pair, writer, context) =>
                {
                    var singleLineFormatter = new SingleLinePlainTextFormatter();
                    writer.Write(pair.Key);
                    singleLineFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(writer, context);
                    return true;
                }),

                new PlainTextFormatter<ReadOnlyMemory<char>>((memory, writer, context) => 
                { 
                    writer.Write(memory.Span.ToString()); 
                    return true;
                }),

                new PlainTextFormatter<Type>((type, writer, context) =>
                {
                    if (type.IsAnonymous())
                    {
                        writer.Write("(anonymous)");
                        return true;
                    }

                    type.WriteCSharpDeclarationTo(writer);
                    return true;
                }),

                new PlainTextFormatter<DateTime>((value, writer, context) =>
                {
                    writer.Write(value.ToString("u"));
                    return true;
                }),

                new PlainTextFormatter<DateTimeOffset>((value, writer, context) =>
                {
                    writer.Write(value.ToString("u"));
                    return true;
                }),

                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: PlainTextFormatter.MimeType,
                    format: ( obj, writer, context) =>
                    {
                        var actualType = obj.GetType();
                        var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                            (actualType.GetGenericArguments());

                        var array = toArray.Invoke(null, new[] { obj });

                        array.FormatTo(writer, context, PlainTextFormatter.MimeType);
                        
                        return true;
                    }),

                new PlainTextFormatter<TextSpan>((span, writer, context) =>
                    {
                        writer.Write(span.ToString(OutputMode.Ansi));
                        return true;
                    }),

                new PlainTextFormatter<JsonElement>((obj, writer, context) =>
                    {
                        writer.Write(obj);
                        return true;
                    }),

                // Fallback for IEnumerable
                new PlainTextFormatter<IEnumerable>((obj, writer, context) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString);
                        return true;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    return formatter.Format(obj, writer, context);
                }),

                // BigInteger should be displayed as plain text
                new PlainTextFormatter<BigInteger>((value, writer, context) =>
                {
                    value.FormatTo(writer, context, PlainTextFormatter.MimeType);
                    return true;
                }),

                // Fallback for any object
                new PlainTextFormatter<object>((obj, writer, context) =>
                {
                    if (obj is null)
                    {
                        writer.Write(Formatter.NullString);
                        return true;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.GetDefaultFormatterForAnyObject(type);
                    return formatter.Format(obj, writer, context);
                })
            };
    }
}