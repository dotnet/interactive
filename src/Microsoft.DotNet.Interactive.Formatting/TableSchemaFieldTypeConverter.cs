// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class TableSchemaFieldTypeConverter : JsonConverter<TableSchemaFieldType>
    {
        public override TableSchemaFieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var tableSchemaFieldType = Enum.TryParse<TableSchemaFieldType>(reader.GetString(), true, out var fieldType) 
                ? fieldType 
                : TableSchemaFieldType.Null;
            reader.Read();
            return tableSchemaFieldType;
        }

        public override void Write(Utf8JsonWriter writer, TableSchemaFieldType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToLowerInvariant());
        }
    }
}