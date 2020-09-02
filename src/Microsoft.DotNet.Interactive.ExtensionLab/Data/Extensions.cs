// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.ML;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Data
{
    public static class Extensions
    {
        public static void Explore(this IDataView source)
        {
            KernelInvocationContext.Current.Display(
                source.ToTabularJsonString(),
                HtmlFormatter.MimeType);
        }

        private static T GetValue<T>(ValueGetter<T> valueGetter)
        {
            T value = default;
            valueGetter(ref value);
            return value;
        }

        public static TabularJsonString ToTabularJsonString(this IDataView source)
        {
            var fields = source.Schema.ToDictionary( column => column.Name, column => column.Type.RawType);
            var data = new JArray();

            var cursor = source.GetRowCursor(source.Schema);

            while (cursor.MoveNext())
            {
                var rowObj = new JObject();

                foreach (var column in source.Schema)
                {
                    var type = column.Type.RawType;
                    var getGetterMethod = cursor.GetType()
                        .GetMethod(nameof(cursor.GetGetter))
                        .MakeGenericMethod(type);

                    var valueGetter = getGetterMethod.Invoke(cursor, new object[] { column });
                   
                    object value = GetValue((dynamic)valueGetter);

                    if (value is ReadOnlyMemory<char>)
                    {
                        value = value.ToString();
                    }

                    var fromObject = JToken.FromObject(value);

                    rowObj.Add(column.Name, fromObject);
                }

                data.Add(rowObj);
            }

            var tabularData = TabularJsonString.Create(fields, data);

            return tabularData;
        }
    }
}
