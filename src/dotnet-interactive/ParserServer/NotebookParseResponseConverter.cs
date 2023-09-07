// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

internal class NotebookParseResponseConverter : JsonConverter<NotebookParserServerResponse>
{
    public override NotebookParserServerResponse Read(
        ref Utf8JsonReader reader, 
        Type typeToConvert, 
        JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        string? id = null;
        InteractiveDocument? document = null;
        byte[]? rawData = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "id":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            id = reader.GetString();
                        }
                        break;

                    case "document":
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                        {
                            document = JsonSerializer.Deserialize<InteractiveDocument>(ref reader, options);
                        }
                        break;

                    case "rawData":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            rawData = JsonSerializer.Deserialize<byte[]>(ref reader, options);
                        }
                        break;

                    default: 
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (id is null)
                {
                    throw new JsonException($"Missing required property 'id' when deserializing {typeof(NotebookParserServerResponse)}");
                }

                if (document is not null)
                {
                    return new NotebookParseResponse(id, document);
                }

                if (rawData is not null)
                {
                    return new NotebookSerializeResponse(id, rawData);
                }

                throw new JsonException($"Cannot deserialize {typeToConvert} due to missing properties");
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }
}