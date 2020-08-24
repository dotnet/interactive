// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class TabularDataSchemaField
    {
        public TabularDataSchemaField(string name, string type)
        {
            Name = name;
            Type = type;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; }
    }
}