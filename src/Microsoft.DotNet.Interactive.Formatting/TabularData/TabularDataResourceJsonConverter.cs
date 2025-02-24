// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

internal class TabularDataResourceJsonConverter : JsonConverter<TabularDataResource>
{
    public override TabularDataResource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var localOptions = new JsonSerializerOptions(options)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        if (localOptions.Converters.FirstOrDefault(c =>
                c is DataDictionaryConverter) is null)
        {
            // ensure dictionaries can be deserialized
            localOptions.Converters.Add(new DataDictionaryConverter());
        }

        EnsureStartObject(reader, typeToConvert);

        TableSchema schema = null;
        List<List<KeyValuePair<string, object>>> data = null;
        string profile = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (schema is { } && data is { })
                {
                    return new TabularDataResource(schema, data, profile);
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
                        case "schema":
                            schema = JsonSerializer.Deserialize<TableSchema>(ref reader, localOptions);
                            break;
                        case "data":
                            data = DeserializeData(ref reader, localOptions, schema);
                            break;
                        case "profile":
                            profile = reader.GetString();
                            break;
                    }

                    break;
            }
        }


        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    private List<List<KeyValuePair<string, object>>> DeserializeData(ref Utf8JsonReader reader,
        JsonSerializerOptions options, TableSchema tableSchema)
    {
        var data = new List<List<KeyValuePair<string, object>>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return data;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var entry = DeserializeDataEntry(ref reader, options, tableSchema);
                data.Add(entry);
            }
        }

        throw new InvalidOperationException();
    }

    private List<KeyValuePair<string, object>> DeserializeDataEntry(ref Utf8JsonReader reader,
        JsonSerializerOptions options, TableSchema tableSchema)
    {
        var entry = new List<KeyValuePair<string, object>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return entry;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    reader.Read();
                    var dataType = tableSchema?.Fields[propertyName].Type;
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.String:
                            entry.Add(new KeyValuePair<string, object>(propertyName, DeserializeStringValueValue(reader.GetString(), dataType ?? TableSchemaFieldType.String)));
                            break;
                        case JsonTokenType.Number:
                            entry.Add(new KeyValuePair<string, object>(propertyName, dataType == TableSchemaFieldType.Integer ? reader.GetInt64() : reader.GetDouble()));
                            break;
                        case JsonTokenType.True:
                            entry.Add(new KeyValuePair<string, object>(propertyName, true));
                            break;
                        case JsonTokenType.False:
                            entry.Add(new KeyValuePair<string, object>(propertyName, false));
                            break;
                        case JsonTokenType.Null:
                            entry.Add(new KeyValuePair<string, object>(propertyName, null));
                            break;
                        case JsonTokenType.StartObject:
                            entry.Add(new KeyValuePair<string, object>(propertyName, JsonSerializer.Deserialize<IDictionary<string, object>>(ref reader, options)));
                            break;
                        case JsonTokenType.StartArray:
                            entry.Add(new KeyValuePair<string, object>(propertyName, DeserializeArray(ref reader, options)));
                            break;
                    }

                    break;
            }

        }
        throw new InvalidOperationException();
    }

    private object[] DeserializeArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var collection = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return collection.ToArray();
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    collection.Add(reader.GetString());
                    break;
                case JsonTokenType.Number:
                    collection.Add(GetNumber(ref reader));
                    break;
                case JsonTokenType.True:
                    collection.Add(true);
                    break;
                case JsonTokenType.False:
                    collection.Add(false);
                    break;
                case JsonTokenType.Null:
                    collection.Add(null);
                    break;
                case JsonTokenType.StartArray:
                    var arrayValue = DeserializeArray(ref reader, options);
                    collection.Add(arrayValue);
                    break;
                case JsonTokenType.StartObject:
                    var objectValue = JsonSerializer.Deserialize<IDictionary<string, object>>(ref reader, options);
                    collection.Add(objectValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        throw new InvalidOperationException();
    }

    private static object GetNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt32(out var integer))
        {
            return integer;
        }

        if (reader.TryGetInt64(out var longInt))
        {
            return longInt;
        }

        if (reader.TryGetDouble(out var doublePrecision))
        {
            return doublePrecision;
        }

        return reader.GetDecimal();
    }

    private object DeserializeStringValueValue(string stringValue, TableSchemaFieldType dataType)
    {
        switch (dataType)
        {
            case TableSchemaFieldType.String:
                return stringValue;
            case TableSchemaFieldType.DateTime:
                return DateTime.Parse(stringValue);
            default:
                throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
        }
    }

    public override void Write(Utf8JsonWriter writer, TabularDataResource value, JsonSerializerOptions options)
    {
        var localOptions = new JsonSerializerOptions(options)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        writer.WriteStartObject();
        writer.WriteString("profile", value.Profile);
        writer.WritePropertyName("schema");
        JsonSerializer.Serialize(writer, value.Schema, localOptions);
        writer.WritePropertyName("data");
        writer.WriteStartArray();
        foreach (var row in value.Data)
        {
            writer.WriteStartObject();
            foreach (var field in row)
            {
                writer.WritePropertyName(field.Key);
                JsonSerializer.Serialize(writer, field.Value, localOptions);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}