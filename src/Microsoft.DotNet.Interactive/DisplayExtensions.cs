// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive;
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
        if (KernelInvocationContext.Current is { } context)
        {
            return context.Display(value, mimeTypes);
        }
        else
        {
            var mimeType = mimeTypes?.FirstOrDefault() ?? "text/plain";
            var output = value.ToDisplayString(mimeType);
            Console.WriteLine(output);
            return new DisplayedValue([new(mimeType, output)], KernelInvocationContext.None);
        }
    }

    public static DisplayedValue DisplayAs(
        this string value,
        string mimeType)
    {
        if (KernelInvocationContext.Current is { } context)
        {
            return context.DisplayAs(value, mimeType);
        }
        else
        {
            Console.WriteLine(value);
            return new DisplayedValue([new(mimeType, value)], KernelInvocationContext.None);
        }
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