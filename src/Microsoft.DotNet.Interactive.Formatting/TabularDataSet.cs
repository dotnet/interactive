// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class TabularDataSet
    {
        public TabularDataSet(TabularDataSchema schema, IEnumerable data)
        {
            Schema = schema;
            Data = data;
        }

        public TabularDataSchema Schema { get; }

        public IEnumerable Data { get; }

        public TabularJsonString ToJson()
        {
            var tabularData = new
            {
                schema = Schema,
                data = Data
            };

            return new TabularJsonString(JsonSerializer.Serialize(tabularData, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }
    }
}