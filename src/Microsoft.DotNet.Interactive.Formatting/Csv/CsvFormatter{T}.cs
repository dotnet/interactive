// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting.Csv;

public class CsvFormatter<T> : TypeFormatter<T>
{
    private readonly FormatDelegate<T> _format;

    public CsvFormatter(FormatDelegate<T> format)
    {
        _format = format;
    }

    public override bool Format(T value, FormatContext context) => _format(value, context);

    public override string MimeType => CsvFormatter.MimeType;

    internal static CsvFormatter<T> Create()
    {
        Func<T, IEnumerable> getHeaderValues = null;

        Func<T, IEnumerable> getRowValues;

        IDestructurer destructurer;
        if (typeof(T).IsEnumerable())
        {
            destructurer = Destructurer.GetOrCreate(typeof(T).GetElementTypeIfEnumerable());
            getRowValues = instance => (IEnumerable)instance;
        }
        else
        {
            destructurer = Destructurer<T>.GetOrCreate();
            getRowValues = instance => new[] { instance };
        }

        var isDictionary =
            typeof(T).GetTypeInfo().ImplementedInterfaces
                     .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) is not null
            ||
            typeof(T).GetTypeInfo().ImplementedInterfaces
                     .FirstOrDefault(i => i == typeof(IDictionary)) is not null;

        if (isDictionary)
        {
            getHeaderValues = instance => ((IDictionary)instance).Keys;
            getRowValues = instance => new[] { instance };
        }
        else
        {
            getHeaderValues = _ => destructurer.Keys;
        }

        return new CsvFormatter<T>( (v,c) => BuildTable(v, getHeaderValues, getRowValues, c));
    }

    internal static bool BuildTable(T value,  Func<T, IEnumerable> getHeaderValues, Func<T, IEnumerable> getRowValues, FormatContext context)
    {
        var keys = getHeaderValues(value);

        if (keys is not ICollection headers)
        {
            return false;
        }

        var headerIndex = 0;

        foreach (var header in headers)
        {
            context.Writer.Write(header);

            if (++headerIndex < headers.Count)
            {
                context.Writer.Write(",");
            }
        }

        context.Writer.WriteLine();

        IDestructurer rowDestructurer = null;

        var rows = getRowValues(value);

        foreach (var row in rows)
        {
            var rowIndex = 0;

            if (row is not IEnumerable cells)
            {
                rowDestructurer ??= Destructurer.GetOrCreate(row.GetType());

                cells = rowDestructurer.Destructure(row).Values;
            }
            else if (row is IDictionary d)
            {
                cells = d.Values;
            }

            foreach (var cell in cells)
            {
                var formatted = cell switch
                {
                    DateTime d => d.ToString("o"),
                    null => "",
                    _ => cell.ToString()
                };

                context.Writer.Write(formatted.EscapeCsvValue());

                if (++rowIndex < headers.Count)
                {
                    context.Writer.Write(",");
                }
            }

            context.Writer.WriteLine();
        }

        return true;
    }

       
}