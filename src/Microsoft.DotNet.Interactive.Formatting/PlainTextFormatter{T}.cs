// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    internal static PlainTextFormatter<T> CreateForAnyObject()
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
                typeof(T).GetMembersToFormat().ToArray()));
    }
  
    internal static PlainTextFormatter<T> CreateForAnyEnumerable()
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
            var joinMethod = typeof(PlainTextFormatter).GetMethod(nameof(PlainTextFormatter.JoinGeneric), BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = joinMethod!.MakeGenericMethod(new[] { t });

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(t);

            var delegateType = typeof(Action<,,>).MakeGenericType(
                new[] { enumerableType, typeof(TextWriter), typeof(FormatContext) });

            var m = genericMethod.CreateDelegate(delegateType);

            return new((value, context) =>
            {
                m.DynamicInvoke(value, context.Writer, context);
                return true;
            });
        }

        return new((value, context) =>
        {
            using var _ = context.IncrementDepth();
            using var __ = context.IncrementDepth();

            switch (value)
            {
                case IEnumerable enumerable:

                    PlainTextFormatter.Join(enumerable,
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

    public static PlainTextFormatter<T> Default = CreateForAnyEnumerable();
}