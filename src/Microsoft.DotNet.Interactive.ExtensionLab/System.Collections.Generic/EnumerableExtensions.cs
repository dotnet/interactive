// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static SandDanceExplorer ExploreWithSandDance<T>(this IEnumerable<T> source)
        {
            var explorer = new SandDanceExplorer();
            explorer.LoadData(source);
            return explorer;
        }
        public static void Explore<T>(this IEnumerable<T> source)
        {
            source.ToTabularJsonString().Explore();
        }

        public static IReadOnlyList<IDictionary<string, object>> ToTable(this IEnumerable<IEnumerable<(string name, object value)>> source)
        {
            var listOfRows = new List<Dictionary<string, object>>();

            foreach (var row in source)
            {
                var dict = new Dictionary<string, object>();

                listOfRows.Add(dict);

                foreach (var (fieldName, fieldValue) in row)
                {
                    dict.Add(fieldName, fieldValue);
                }
            }

            return listOfRows;
        }  
        
        public static IEnumerable<IReadOnlyList<IDictionary<string, object>>> ToTables(this IEnumerable<IEnumerable<IEnumerable<(string, object)>>> source) => 
            source.Select(x => x.ToTable());
    }
}