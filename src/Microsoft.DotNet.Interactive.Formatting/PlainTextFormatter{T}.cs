// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting;

public delegate bool FormatDelegate<in T>(T value, FormatContext context);

public class PlainTextFormatter<T> : TypeFormatter<T>
{
    private readonly FormatDelegate<T> _format;

    public PlainTextFormatter(FormatDelegate<T> format)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
    }

    public PlainTextFormatter(Action<T, FormatContext> format)
    {
        _format = FormatInstance;

        bool FormatInstance(T instance, FormatContext context)
        {
            format(instance, context);
            return true;
        }
    }

    public PlainTextFormatter(Func<T, string> format)
    {
        _format = (instance, context) =>
        {
            context.Writer.Write(format(instance));
            return true;
        };
    }

    public override string MimeType => PlainTextFormatter.MimeType;

    public override bool Format(T value, FormatContext context)
    {
        if (value is null)
        {
            context.Writer.Write(Formatter.NullString);
            return true;
        }

        return _format(value, context);
    }

    public static PlainTextFormatter<T> CreateForAnyObject(bool includeInternals = false)
    {
        if (typeof(T).IsScalar())
        {
            return new PlainTextFormatter<T>((value, context) =>
            {
                context.Writer.Write(value);
                return true;
            });
        }

        return new PlainTextFormatter<T>(
            PlainTextFormatter.CreateFormatDelegate<T>(
                typeof(T).GetMembersToFormat(includeInternals).ToArray()));
    }

    public static PlainTextFormatter<T> CreateForMembers(params Expression<Func<T, object>>[] members)
    {
        var format = PlainTextFormatter.CreateFormatDelegate<T>(
            typeof(T).GetMembers(members).ToArray());

        return new PlainTextFormatter<T>(format);
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Part of Pattern")]
    public static PlainTextFormatter<T> CreateForAnyEnumerable(bool includeInternals = false)
    {
        if (typeof(T) == typeof(string))
        {
            return new((value, context) =>
            {
                context.Writer.Write(value);
                return true;
            });
        }

        if (typeof(T).GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(i => i.GenericTypeArguments[0])
                .FirstOrDefault() is { } t)
        {
            var joinMethod = typeof(Formatter).GetMethod(nameof(Formatter.JoinGeneric), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = joinMethod.MakeGenericMethod(new[] { t });

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(t);

            var delegateType = typeof(Action<,,,>).MakeGenericType(
                new[] { enumerableType, typeof(TextWriter), typeof(FormatContext), typeof(int?) });

            var m = genericMethod.CreateDelegate(delegateType);

            return new((value, context) =>
            {
                m.DynamicInvoke(value, context.Writer, context, Formatter<T>.ListExpansionLimit);
                return true;
            });
        }

        return new((value, context) =>
        {
            switch (value)
            {
                case IEnumerable enumerable:
                    Formatter.Join(enumerable,
                        context.Writer,
                        context);
                    break;
                default:
                    context.Writer.Write(value.ToString());
                    break;
            }

            return true;
        });
    }

    public static PlainTextFormatter<T> Default = CreateForAnyEnumerable(false);
}