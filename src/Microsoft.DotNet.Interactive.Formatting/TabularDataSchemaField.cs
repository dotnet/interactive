// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class TabularDataSchemaField
    {
        public TabularDataSchemaField(string name, string type)
        {
            Name = name;
            Type = type;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName( "type")]
        public string Type { get; }
    }
}