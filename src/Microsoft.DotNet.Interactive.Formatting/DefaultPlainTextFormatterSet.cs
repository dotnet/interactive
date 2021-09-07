// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
                new PlainTextFormatter<ExpandoObject>((expando, context) =>
                    {
                        var singleLineFormatter = new SingleLinePlainTextFormatter();
                        singleLineFormatter.WriteStartObject(context.Writer);
                        var pairs = expando.ToArray();
                        var length = pairs.Length;
                        for (var i = 0; i < length; i++)
                        {
                            var pair = pairs[i];
                            context.Writer.Write(pair.Key);
                            singleLineFormatter.WriteNameValueDelimiter(context.Writer);
                            pair.Value.FormatTo(context);

                            if (i < length - 1)
                            {
                                singleLineFormatter.WritePropertyDelimiter(context.Writer);
                            }
                        }

                        singleLineFormatter.WriteEndObject(context.Writer);
                        return true;
                    }),

                new PlainTextFormatter<IHtmlContent>((view, context) =>
                {
                    view.WriteTo(context.Writer, HtmlEncoder.Default);
                    return true;
                }),

                new PlainTextFormatter<KeyValuePair<string, object>>((pair, context) =>
                {
                    var singleLineFormatter = new SingleLinePlainTextFormatter();
                    context.Writer.Write(pair.Key);
                    singleLineFormatter.WriteNameValueDelimiter(context.Writer);
                    pair.Value.FormatTo(context);
                    return true;
                }),

                new PlainTextFormatter<ReadOnlyMemory<char>>((memory, context) => 
                {
                    context.Writer.Write(memory.Span.ToString()); 
                    return true;
                }),

                new PlainTextFormatter<Type>((type, context) =>
                {
                    if (type.IsAnonymous())
                    {
                        context.Writer.Write("(anonymous)");
                        return true;
                    }

                    type.WriteCSharpDeclarationTo(context.Writer);
                    return true;
                }),

                new PlainTextFormatter<DateTime>((value, context) =>
                {
                    context.Writer.Write(value.ToString("u"));
                    return true;
                }),

                new PlainTextFormatter<DateTimeOffset>((value, context) =>
                {
                    context.Writer.Write(value.ToString("u"));
                    return true;
                }),

                new AnonymousTypeFormatter<object>(type: typeof(ReadOnlyMemory<>),
                    mimeType: PlainTextFormatter.MimeType,
                    format: ( obj, context) =>
                    {
                        var actualType = obj.GetType();
                        var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                            (actualType.GetGenericArguments());

                        var array = toArray.Invoke(null, new[] { obj });

                        array.FormatTo(context, PlainTextFormatter.MimeType);
                        
                        return true;
                    }),

                new PlainTextFormatter<JsonElement>((obj, context) =>
                    {
                        context.Writer.Write(obj);
                        return true;
                    }),

                // Fallback for IEnumerable
                new PlainTextFormatter<IEnumerable>((obj, context) =>
                {
                    if (obj is null)
                    {
                        context.Writer.Write(Formatter.NullString);
                        return true;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.GetDefaultFormatterForAnyEnumerable(type);
                    return formatter.Format(obj, context);
                }),

                // BigInteger should be displayed as plain text
                new PlainTextFormatter<BigInteger>((value, context) =>
                {
                    value.FormatTo(context, PlainTextFormatter.MimeType);
                    return true;
                }),

                // Fallback for any object
                new PlainTextFormatter<object>((obj, context) =>
                {
                    if (obj is null)
                    {
                        context.Writer.Write(Formatter.NullString);
                        return true;
                    }
                    var type = obj.GetType();
                    var formatter = PlainTextFormatter.GetDefaultFormatterForAnyObject(type);
                    return formatter.Format(obj, context);
                })
            };
    }
}