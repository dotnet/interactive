// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    public class NotebookParseResponseConverter : JsonConverter<NotebookParserServerResponse>
    {
        public override NotebookParserServerResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureStartObject(reader, typeToConvert);

            string id = null;
            InteractiveDocument document = null;
            byte[] rawData = null;
            string errorMessage = null;

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
                        case "errorMessage":
                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                            {
                                errorMessage = reader.GetString();
                            }
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (id is null)
                    {
                        throw new JsonException("Missing properties on response object");
                    }

                    if (document is not null)
                    {
                        return new NotebookParseResponse(id, document);
                    }

                    if (rawData is not null)
                    {
                        return new NotebookSerializeResponse(id, rawData);
                    }

                    if (errorMessage is not null)
                    {
                        return new NotebookErrorResponse(id, errorMessage);
                    }

                    throw new JsonException($"Cannot deserialize {typeToConvert.Name} due to missing properties");
                }
            }

            throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
        }
    }
}
