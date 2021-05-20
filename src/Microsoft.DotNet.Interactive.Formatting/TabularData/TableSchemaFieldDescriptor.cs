// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData
{
    public class TableSchemaFieldDescriptor
    {
        public TableSchemaFieldDescriptor(string name, TableSchemaFieldType? type = TableSchemaFieldType.Null, string description = null, string format= null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Description = description;
            Format = format;
            Type = type?? TableSchemaFieldType.Null;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Description { get; }

        [JsonPropertyName("format"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Format { get; }

        [JsonPropertyName( "type")]
        public TableSchemaFieldType Type { get; }
    }
}