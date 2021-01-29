// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public class DataDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureStartObject(reader,typeToConvert);

            var value = new Dictionary<string, object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return value;
                }

                var keyString = reader.GetString();
                object itemValue;

                reader.Read();

                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        itemValue = reader.GetString();
                        break;
                    case JsonTokenType.Number:
                        itemValue = reader.GetDouble();
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                value.Add(keyString, itemValue);
            }

            throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
        }
    }
}