// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularDataResource
    {
        public string Profile { get; }

        public TableSchema Schema { get; }

        public IEnumerable Data { get; }

        public TabularDataResource(TableSchema schema, IEnumerable data)
        {
            Profile = "tabular-data-resource";
            Schema = schema;
            Data = data;
        }

        public TabularDataResourceJsonString ToJson()
        {
            return new(JsonSerializer.Serialize(this, TabularDataResourceFormatter.JsonSerializerOptions));
        }
    }
}