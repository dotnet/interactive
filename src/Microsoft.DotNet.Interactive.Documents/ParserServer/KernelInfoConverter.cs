﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer;

internal class KernelInfoConverter : JsonConverter<KernelInfo>
{
    public override KernelInfo Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        string? name = null;
        string[]? aliases = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "name":
                        name = reader.ReadString();

                        break;
                    case "aliases":
                        aliases = reader.ReadArray<string>(options);

                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
        }

        return new(name!, aliases);
    }

    public override void Write(Utf8JsonWriter writer, KernelInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("name");
        writer.WriteStringValue(value.Name);

        if (value.Aliases.Count > 0)
        {
            writer.WritePropertyName("aliases");
            writer.WriteStartArray();

            foreach (var alias in value.Aliases)
            {
                writer.WriteStringValue(alias);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}