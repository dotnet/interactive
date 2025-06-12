// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace System;

public static class DisplayExtensions
{
    /// <summary>
    /// Formats an object using the <see cref="Formatter"/> into a string to be displayed. 
    /// </summary>
    /// <param name="value">The value to display.</param>
    /// <param name="mimeTypes">The MIME types.</param>
    /// <returns>An instance of <see cref="DisplayedValue"/> that can be used to later update the display.</returns>
    public static DisplayedValue Display(
        this object value,
        params string[] mimeTypes)
    {
        var formattedValues = FormattedValue.CreateManyFromObject(value, mimeTypes);
        Formatter.RaiseFormatterEvent(new(value, formattedValues));
        return new DisplayedValue(value, formattedValues);
    }

    public static DisplayedValue DisplayAs(
        this string value,
        string mimeType)
    {
        IReadOnlyList<FormattedValue> formattedValues = [new(mimeType, value)];
        Formatter.RaiseFormatterEvent(new(value, formattedValues));
        return new DisplayedValue(value, formattedValues);
    }

    public static DisplayedValue DisplayTable<T>(
        this IEnumerable<T> value,
        params string[] mimeTypes)
    {
        if (value is null)
        {
            return value.Display(mimeTypes);
        }

        var tabularDataResource = value.ToTabularDataResource();

        return tabularDataResource.Display(mimeTypes);
    }
}