// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.ML;

namespace Microsoft.DotNet.Interactive.Data
{

    public class DataFrameKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            RegisterDataFrameFormatters();
            return Task.CompletedTask;
        }

        public static void RegisterDataFrameFormatters()
        {
            Formatter.Register<IDataView>((dataView, writer) =>
            {
                var tabularData = dataView.ToTabularJsonString();
                writer.Write(tabularData.ToString());
            }, TabularDataResourceFormatter.MimeType);

            Formatter.Register<IDataView>((dataView, writer) =>
            {
                var tabularData = dataView.ToTabularJsonString();
                writer.Write(tabularData.ToString());
            }, JsonFormatter.MimeType);
        }
    }
}

namespace Microsoft.ML
{

    public static class DataViewExtensions
    {
        private static T GetValue<T>(ValueGetter<T> valueGetter)
        {
            T value = default;
            valueGetter(ref value);
            return value;
        }

        public static TabularDataResource ToTabularDataResource(this IDataView source)
        {
            var fields = source.Schema.ToDictionary(column => column.Name, column => column.Type.RawType);
            var data = new List<IEnumerable<KeyValuePair<string, object>>>();

            var cursor = source.GetRowCursor(source.Schema);

            while (cursor.MoveNext())
            {
                var rowObj = new List<KeyValuePair<string, object>>();

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

                    rowObj.Add(new KeyValuePair<string, object>(column.Name, value));
                }

                data.Add(rowObj);
            }

            var schema = new TableSchema();

            foreach (var (fieldName, fieldValue) in fields)
            {
                schema.Fields.Add(new TableSchemaFieldDescriptor(fieldName, fieldValue.ToTableSchemaFieldType()));
            }

            return new TabularDataResource(schema, data);
        }

        public static TabularDataResourceJsonString ToTabularJsonString(this IDataView source)
        {
            var tabularDataResource = source.ToTabularDataResource();
            return tabularDataResource.ToJsonString();
        }
    }
}