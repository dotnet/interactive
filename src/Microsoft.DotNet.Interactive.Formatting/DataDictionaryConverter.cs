// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting;

internal class DataDictionaryConverter : JsonConverter<IDictionary<string, object>>
{
    public override IDictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        var value = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return value;
            }

            var keyString = reader.GetString();

            reader.Read();

            value.Add(keyString ?? throw new InvalidOperationException(), GetValue(ref reader, options));
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    private static object GetValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        object itemValue;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                itemValue = reader.GetString();
                break;
            case JsonTokenType.Number:
                itemValue = GetNumber(ref reader);
                break;
            case JsonTokenType.True:
                itemValue = true;
                break;
            case JsonTokenType.False:
                itemValue = false;
                break;
            case JsonTokenType.Null:
                itemValue = null;
                break;
            case JsonTokenType.StartArray:
                itemValue = ParseArray(ref reader, options);
                break;
            case JsonTokenType.StartObject:
                itemValue = JsonSerializer.Deserialize(ref reader, typeof(IDictionary<string, object>), options);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return itemValue;

        static object GetNumber(ref Utf8JsonReader reader)
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
    }

    private static object ParseArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var values = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToArray();
            }

            values.Add(GetValue(ref reader, options));
        }

        throw new JsonException("Missing end of array.");
    }
}