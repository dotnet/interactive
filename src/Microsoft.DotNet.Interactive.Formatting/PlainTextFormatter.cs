// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.Formatting;

public static class PlainTextFormatter
{
    public const string MimeType = "text/plain";

    public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
        Formatter.GetPreferredFormatterFor(type, MimeType);

    public static ITypeFormatter GetPreferredFormatterFor<T>() =>
        GetPreferredFormatterFor(typeof(T));

    private const int NumberOfSpacesToIndent = 2;

    internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type, bool includeInternals = false) =>
        FormattersForAnyObject.GetOrCreateFormatterForType(type);

    internal static FormatDelegate<T> CreateFormatDelegate<T>(MemberInfo[] forMembers)
    {
        var accessors = forMembers.GetMemberAccessors<T>().ToArray();

        if (Formatter<T>.TypeIsValueTuple ||
            Formatter<T>.TypeIsTuple)
        {
            return FormatTuple;
        }

        if (typeof(T).IsEnum)
        {
            return (enumValue, context) =>
            {
                context.Writer.Write(enumValue.ToString());
                return true;
            };
        }

        return FormatObject;

        bool FormatObject(T target, FormatContext context)
        {
            // If we haven't got any members to show, just resort to ToString()
            if (accessors.Length == 0)
            {
                context.Writer.Write(target.ToString());
                return true;
            }

            var indent = context.Indent;

            if (!context.IsStartingObjectWithinSequence)
            {
                var type = target.GetType();
                type.WriteCSharpDeclarationTo(context.Writer, true);
                context.Writer.WriteLine();
            }

            for (var i = 0; i < accessors.Length; i++)
            {
                var accessor = accessors[i];

                object value = accessor.GetValueOrException(target);

                WriteStartProperty(context);
                context.Writer.Write(accessor.Member.Name);
                context.Writer.Write(": ");
                value.FormatTo(context);

                if (i < accessors.Length - 1)
                {
                    context.Writer.WriteLine();
                }
            }

            if (accessors.Length < accessors.Length)
            {
                WriteIndent(context);
                context.Writer.Write("...");
            }

            context.Indent = indent;

            return true;
        }

        bool FormatTuple(T target, FormatContext context)
        {
            if (Formatter<T>.TypeIsTupleOfScalars)
            {
                context.Writer.Write("( ");

                for (var i = 0; i < accessors.Length; i++)
                {
                    var value = accessors[i].GetValueOrException(target);

                    value.FormatTo(context);

                    if (i < accessors.Length - 1)
                    {
                        context.Writer.Write(", ");
                    }
                }

                context.Writer.Write(" )");
            }
            else
            {
                for (var i = 0; i < accessors.Length; i++)
                {
                    var value = accessors[i].GetValueOrException(target);

                    context.IsStartingObjectWithinSequence = true;

                    WriteStartProperty(context);

                    value.FormatTo(context);

                    context.IsStartingObjectWithinSequence = true;

                    if (i < accessors.Length - 1)
                    {
                        context.Writer.WriteLine();
                    }
                }
            }

            return true;
        }
    }

    internal static FormatterMapByType FormattersForAnyObject =
        new(typeof(PlainTextFormatter<>), nameof(PlainTextFormatter<object>.CreateForAnyObject));

    internal static FormatterMapByType FormattersForAnyEnumerable =
        new(typeof(PlainTextFormatter<>), nameof(PlainTextFormatter<object>.CreateForAnyEnumerable));

    internal static ITypeFormatter[] DefaultFormatters =
    {
        new PlainTextFormatter<Exception>((exception, context) =>
        {
            context.Writer.Write(exception.ToString().IndentAtNewLines(context));

            return true;
        }),

        new PlainTextFormatter<ExpandoObject>((expando, context) =>
        {
            var pairs = expando.ToArray();
            var length = pairs.Length;
            for (var i = 0; i < length; i++)
            {
                var pair = pairs[i];
                context.Writer.Write(pair.Key);
                context.Writer.Write(": ");
                pair.Value.FormatTo(context);

                if (i < length - 1)
                {
                    context.Writer.WriteLine();
                }
            }

            return true;
        }),

        new PlainTextFormatter<IHtmlContent>((view, context) =>
        {
            view.WriteTo(context.Writer, HtmlEncoder.Default);
            return true;
        }),

        new PlainTextFormatter<KeyValuePair<string, object>>((pair, context) =>
        {
            context.Writer.Write(pair.Key);
            context.Writer.Write(": ");
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
                                           mimeType: MimeType,
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
            var formatter = FormattersForAnyEnumerable.GetOrCreateFormatterForType(type);
            return formatter.Format(obj, context);
        }),

        // BigInteger should be displayed as plain text
        new PlainTextFormatter<BigInteger>((value, context) =>
        {
            value.FormatTo(context, PlainTextFormatter.MimeType);
            return true;
        }),

        // Decimal should be displayed as plain text
        new PlainTextFormatter<decimal>((value, context) =>
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
            var formatter = GetDefaultFormatterForAnyObject(type);
            return formatter.Format(obj, context);
        })
    };

    private static string IndentAtNewLines(this string s, FormatContext context) =>
        Regex.Replace(s, @"^\s+", new string(' ', (context.Depth + 1) * NumberOfSpacesToIndent), RegexOptions.Multiline);

    internal static void WriteIndent(FormatContext context, string bonus = "    ")
    {
        var effectiveIndent = context.Depth * NumberOfSpacesToIndent;
        var indent = new string(' ', effectiveIndent);
        context.Writer.Write(indent);
        context.Writer.Write(bonus);
    }

    public static void WriteStartProperty(FormatContext context)
    {
        if (context.IsStartingObjectWithinSequence)
        {
            WriteIndent(context, "  - ");
            context.IsStartingObjectWithinSequence = false;
        }
        else
        {
            WriteIndent(context);
        }
    }
}