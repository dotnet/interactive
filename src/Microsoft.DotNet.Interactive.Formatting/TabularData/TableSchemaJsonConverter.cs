// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

internal class TableSchemaJsonConverter : JsonConverter<TableSchema>
{
    public override TableSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        var primaryKey = Array.Empty<string>();
        TableDataFieldDescriptors fields = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (fields is { })
                {
                    var schema = new TableSchema();
                    schema.PrimaryKey.AddRange(primaryKey ?? Array.Empty<string>());
                    foreach (var tableDataFieldDescriptor in fields)
                    {
                        schema.Fields.Add(tableDataFieldDescriptor);
                    }
                    return schema;

                }
                throw new JsonException($"Cannot deserialize {typeToConvert.Name}");

            }

            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "primaryKey":
                            primaryKey = JsonSerializer.Deserialize<string[]>(ref reader, options);
                            break;
                        case "fields":
                            fields = JsonSerializer.Deserialize<TableDataFieldDescriptors>(ref reader, options);
                            break;
                    }

                    break;
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(Utf8JsonWriter writer, TableSchema value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("primaryKey");
        JsonSerializer.Serialize(writer, value.PrimaryKey.ToArray(), options);
        writer.WritePropertyName("fields");
        JsonSerializer.Serialize(writer, value.Fields, options);
        writer.WriteEndObject();
    }
}