// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

internal class NotebookParseRequestConverter : JsonConverter<NotebookParseOrSerializeRequest>
{
    public override NotebookParseOrSerializeRequest Read(
        ref Utf8JsonReader reader, 
        Type typeToConvert, 
        JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        RequestType? type = null;
        string? id = null;
        DocumentSerializationType? serializationType = null;
        string? defaultLanguage = null;
        byte[]? rawData = null;
        string? newLine = null;
        InteractiveDocument? document = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "type":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            type = JsonSerializer.Deserialize<RequestType>(ref reader, options);
                        }
                        break;

                    case "id":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            id = reader.GetString();
                        }
                        break;

                    case "serializationType":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            serializationType = JsonSerializer.Deserialize<DocumentSerializationType>(ref reader, options);
                        }
                        break;

                    case "defaultLanguage":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            defaultLanguage = reader.GetString();
                        }
                        break;

                    case "rawData":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            rawData = JsonSerializer.Deserialize<byte[]>(ref reader, options);
                        }
                        break;

                    case "newLine":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            newLine = reader.GetString();
                        }
                        break;

                    case "document":
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                        {
                            document = JsonSerializer.Deserialize<InteractiveDocument>(ref reader, options);
                        }
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (type is null ||
                    id is null ||
                    serializationType is null ||
                    defaultLanguage is null)
                {
                    throw new JsonException($"""
                    Unable to deserialize {typeof(NotebookParseOrSerializeRequest)}.
                        {nameof(type)} = {type}
                        {nameof(id)} = {id}
                        {nameof(serializationType)} = {serializationType}
                        {nameof(defaultLanguage)} = {defaultLanguage}
                    """);
                }

                switch (type.GetValueOrDefault())
                {
                    case RequestType.Parse:
                        if (rawData is null)
                        {
                            throw new JsonException($"Missing property for {nameof(NotebookParseRequest)}");
                        }

                        return new NotebookParseRequest(id, serializationType.GetValueOrDefault(), defaultLanguage, rawData);

                    case RequestType.Serialize:
                        if (newLine is null ||
                            document is null)
                        {
                            throw new JsonException($"Missing property for {nameof(NotebookSerializeRequest)}");
                        }

                        return new NotebookSerializeRequest(id, serializationType.GetValueOrDefault(), defaultLanguage, newLine, document);

                    default:
                        throw new JsonException($"Unsupported request type '{type}'");
                }
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }
}