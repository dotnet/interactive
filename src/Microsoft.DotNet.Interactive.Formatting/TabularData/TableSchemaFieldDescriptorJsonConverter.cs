// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

internal class TableSchemaFieldDescriptorJsonConverter : JsonConverter<TableSchemaFieldDescriptor>
{
    public override TableSchemaFieldDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        string name = null;
        string description = null;
        string format = null;
        TableSchemaFieldType type = TableSchemaFieldType.Any;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    return new TableSchemaFieldDescriptor(name, type, description, format);
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
                        case "name":
                            name = reader.GetString();
                            break;
                        case "description":
                            description = reader.GetString();
                            break;
                        case "format":
                            format = reader.GetString();
                            break;
                        case "type":
                            type = JsonSerializer.Deserialize<TableSchemaFieldType>(ref reader, options);
                            break;
                    }
                    break;
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(Utf8JsonWriter writer, TableSchemaFieldDescriptor value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("type");
        JsonSerializer.Serialize(writer, value.Type, options);

        writer.WriteString("name", value.Name);
        if (!string.IsNullOrEmpty(value.Description))
        {
            writer.WriteString("description", value.Description);
        }
        if (!string.IsNullOrEmpty(value.Format))
        {
            writer.WriteString("format", value.Format);
        }
        writer.WriteEndObject();
    }
}