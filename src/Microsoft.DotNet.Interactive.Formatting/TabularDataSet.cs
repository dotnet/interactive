// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularDataSet
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
            var schema = JObject.FromObject(Schema);
            var data = JArray.FromObject(Data);

            var tabularData = new JObject
            {
                ["schema"] = schema,
                ["data"] = data
            };

            return new TabularJsonString(tabularData.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}