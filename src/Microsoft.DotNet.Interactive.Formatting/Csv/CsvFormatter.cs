// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace Microsoft.DotNet.Interactive.Formatting.Csv;

public static class CsvFormatter
{
    public const string MimeType = "text/csv";

    internal static ITypeFormatter[] DefaultFormatters =
    {
        new CsvFormatter<IEnumerable>((seq, context) =>
        {
            ITypeFormatter formatter = FormattersForAnyEnumerable.GetOrCreateFormatterForType(seq.GetType());

            formatter.Format(seq, context);

            return true;
        }),

        new CsvFormatter<object>((obj, context) =>
        {
            var formatter = FormattersForAnyEnumerable.GetOrCreateFormatterForType(obj.GetType());

            formatter.Format(obj, context);

            return true;
        }),
    };

    public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
        Formatter.GetPreferredFormatterFor(type, MimeType);

    public static ITypeFormatter<T> GetPreferredFormatterFor<T>() =>
        (ITypeFormatter<T>)Formatter.GetPreferredFormatterFor(typeof(T), MimeType);

    internal static FormatterMapByType FormattersForAnyEnumerable =
        new(typeof(CsvFormatter<>), nameof(CsvFormatter<object>.Create));

    internal static string EscapeCsvValue(this string value)
    {
        var input = value.AsMemory();

        value = value.Replace("\"", "\"\"");

        if (ShouldBeWrappedInQuotes())
        {
            value = $"\"{value}\"";
        }

        return value;

        bool ShouldBeWrappedInQuotes()
        {
            for (var i = 0; i < input.Length; i++)
            {
                var v = value[i];

                if (char.IsWhiteSpace(v))
                {
                    return true;
                }
            }

            return false;
        }
    }
}