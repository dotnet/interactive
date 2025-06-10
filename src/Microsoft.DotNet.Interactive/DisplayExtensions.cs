// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace System;

public static class DisplayExtensions
{
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