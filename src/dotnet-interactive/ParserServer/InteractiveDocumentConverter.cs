// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

internal class InteractiveDocumentConverter : JsonConverter<InteractiveDocument>
{
    public override InteractiveDocument Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        var document = new InteractiveDocument();

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "elements":
                        var cells = reader.ReadArray<InteractiveDocumentElement>(options);

                        if (cells is not null)
                        {
                            foreach (var cell in cells)
                            {
                                document.Elements.Add(cell);
                            }
                        }

                        break;

                    case "metadata":
                        var metadata = reader.ReadDataDictionary(options);
                        if (metadata is not null)
                        {
                            document.Metadata.MergeWith(metadata);
                        }

                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }
        }

        return document;
    }

    public override void Write(
        Utf8JsonWriter writer,
        InteractiveDocument document,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("elements");

        writer.WriteStartArray();

        foreach (var element in document.Elements)
        {
            JsonSerializer.Serialize(writer, element, options);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("metadata");
        JsonSerializer.Serialize(writer, document.Metadata, options);
        
        writer.WriteEndObject();
    }
}