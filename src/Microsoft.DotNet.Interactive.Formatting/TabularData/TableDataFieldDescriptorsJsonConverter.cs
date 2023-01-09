// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

public class TableDataFieldDescriptorsJsonConverter : JsonConverter<TableDataFieldDescriptors>
{
    public override TableDataFieldDescriptors Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        EnsureStartArray(reader, typeToConvert);

        var fields = new List<TableSchemaFieldDescriptor>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                var ret = new TableDataFieldDescriptors();
                foreach (var field in fields)
                {
                    ret.Add(field);
                }
                return ret;

            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var value = JsonSerializer.Deserialize<TableSchemaFieldDescriptor>(ref reader, options);

                fields.Add(value);
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(Utf8JsonWriter writer, TableDataFieldDescriptors value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var field in value)
        {
            JsonSerializer.Serialize(writer, field, field.GetType(), options);
        }
        writer.WriteEndArray();
    }
}