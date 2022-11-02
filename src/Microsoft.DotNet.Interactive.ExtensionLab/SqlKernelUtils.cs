// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

internal static class SqlKernelUtils
{
    /// <summary>
    /// Appends a number to the end of duplicate column names in the provided
    /// string array. Some column names in SQL query results can be duplicates
    /// either from deliberate aliasing with the AS keyword, or from being
    /// unnamed columns, but column names have to be unique in a Table Schema.
    /// </summary>
    /// <param name="columnNames">
    /// The array of column names to be aliased. Note: The provided array will
    /// be modified by this method.
    /// </param>
    internal static void AliasDuplicateColumnNames(string[] columnNames)
    {
        var nameCounts = new Dictionary<string, int>(capacity: columnNames.Length);
        for (var i = 0; i < columnNames.Length; i++)
        {
            var columnName = columnNames[i];
            if (nameCounts.TryGetValue(columnName, out var count))
            {
                nameCounts[columnName] = ++count;
                columnNames[i] = columnName + $" ({count})";
            }
            else
            {
                nameCounts[columnName] = 1;
            }
        }
    }
}