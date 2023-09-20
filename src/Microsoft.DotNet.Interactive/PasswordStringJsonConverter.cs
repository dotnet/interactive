// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive;

internal class PasswordStringJsonConverter : JsonConverter<PasswordString>
{
    public override PasswordString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);
        string password = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (password is not null)
                {
                    return new PasswordString(password);
                }

                throw new JsonException($"Cannot deserialize {typeToConvert.Name}");

            }

            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    reader.Read();
                    password = propertyName switch
                    {
                        "password" => reader.GetString(),
                        _ => password
                    };

                    break;
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(Utf8JsonWriter writer, PasswordString value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("password", value.GetClearTextPassword());
        writer.WriteEndObject();
    }
}