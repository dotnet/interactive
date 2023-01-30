// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic;

public static class EnumerableExtensions
{

    public static IReadOnlyList<IEnumerable<KeyValuePair<string, object>>> ToTable(this IEnumerable<IEnumerable<(string name, object value)>> source)
    {
        var listOfRows = new List<IEnumerable<KeyValuePair<string, object>>>();

        foreach (var row in source)
        {
            var dict = new List<KeyValuePair<string, object>>();

            listOfRows.Add(dict);

            foreach (var (fieldName, fieldValue) in row)
            {
                dict.Add( new KeyValuePair<string, object>( fieldName, fieldValue));
            }
        }

        return listOfRows;
    }  
        
    public static IEnumerable<IReadOnlyList<IEnumerable<KeyValuePair<string, object>>>> ToTables(this IEnumerable<IEnumerable<IEnumerable<(string, object)>>> source) => 
        source.Select(x => x.ToTable());
}