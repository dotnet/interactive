// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace Microsoft.DotNet.Interactive.Formatting;

public static class PlainTextSummaryFormatter
{
    // FIX: (PlainTextSummaryFormatter) rename this
    public const string MimeType = "text/plain+summary";

    public static ITypeFormatter GetPreferredFormatterFor(Type type)
    {
        return Formatter.GetPreferredFormatterFor(type, MimeType);
    }

    internal static ITypeFormatter[] DefaultFormatters { get; } =
    {
        new AnonymousTypeFormatter<Type>((value, context) =>
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor<Type>();

            return formatter.Format(value, context);
        }, MimeType),
        
        new AnonymousTypeFormatter<string>((value, context) =>
        {
            if (value is null)
            {
                context.Writer.Write(Formatter.NullString);
                return true;
            }

            context.Writer.Write(Truncate(value));
            return true;

        }, MimeType),
        
        new AnonymousTypeFormatter<IEnumerable>((obj, context) =>
        {
            if (obj is null)
            {
                context.Writer.Write(Formatter.NullString);
                return true;
            }

            var type = obj.GetType();
            var formatter = FormattersForAnyEnumerable.GetOrCreateFormatterForType(type);

            context.DisableRecursion();

            var result = formatter.Format(obj, context);

            context.EnableRecursion();

            return result;
        }, MimeType),

        new AnonymousTypeFormatter<object>((value, context) =>
        {
            if (value is null)
            {
                context.Writer.Write(Formatter.NullString);
            }
            else
            {
                try
                {
                    context.Writer.Write(Truncate(value));
                }
                catch (Exception exception)
                {
                    var formatter = GetPreferredFormatterFor(exception.GetType());
                    formatter.Format(exception, context);
                }
            }

            return true;
        }, MimeType)
    };

    private static string Truncate(object value)
    {
        var formatted = value.ToString()
                             .Trim()
                             .Replace("\r", "\\r")
                             .Replace("\n", "\\n");

        var length = 300;

        if (formatted.Length > length)
        {
            formatted = formatted[..length] + "...";
        }

        return formatted;
    }

    internal static FormatterMapByType FormattersForAnyEnumerable =
        new(typeof(PlainTextFormatter<>), nameof(PlainTextFormatter<object>.CreateForAnyEnumerable));
}