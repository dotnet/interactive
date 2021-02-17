// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static void Explore<T>(this IEnumerable<T> source)
        {
            KernelInvocationContext.Current.Display(
                source.ToTabularJsonString(),
                HtmlFormatter.MimeType);
        }

        public static IReadOnlyList<IDictionary<string, object>> ToTable(this IEnumerable<IEnumerable<(string name, object value)>> source)
        {
            var listOfRows = new List<Dictionary<string, object>>();

            foreach (var row in source)
            {
                var dict = new Dictionary<string, object>();

                listOfRows.Add(dict);

                foreach (var field in row)
                {
                    dict.Add(field.name, field.value);
                }
            }

            return listOfRows;
        }  
        
        public static IEnumerable<IReadOnlyList<IDictionary<string, object>>> ToTables(this IEnumerable<IEnumerable<IEnumerable<(string, object)>>> source) => 
            source.Select(x => x.ToTable());
    }
}