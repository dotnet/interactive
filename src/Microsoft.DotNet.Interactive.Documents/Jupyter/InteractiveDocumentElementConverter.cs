// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.Utility;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

internal class InteractiveDocumentElementConverter : JsonConverter<InteractiveDocumentElement>
{
    public override InteractiveDocumentElement Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        InteractiveDocumentElement element = new();
        string? inferredTargetKernelName = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "cell_type":
                        var cellType = reader.ReadString();
                        if (cellType == "markdown")
                        {
                            element.Language = "markdown";
                        }

                        break;

                    case "execution_count":
                        element.ExecutionCount = reader.ReadInt32() ?? 0;
                        break;

                    case "id":
                        element.Id = reader.ReadString();
                        break;

                    case "metadata":
                        var metadata = reader.ReadDataDictionary(options);
                        element.Metadata ??= new Dictionary<string, object>();
                        element.Metadata.MergeWith(metadata);

                        if (element.Metadata?.TryGetValue("dotnet_interactive", out var dotnet_interactive) == true &&
                            dotnet_interactive is IDictionary<string, object> dotnet_interactive_dict &&
                            dotnet_interactive_dict.TryGetValue("language", out var languageStuff) &&
                            languageStuff is string language)
                        {
                            element.Language = language;
                        }

                        break;

                    case "outputs":
                        var outputs = reader.ReadArray<InteractiveDocumentOutputElement>(options);

                        if (outputs is { })
                        {
                            foreach (var output in outputs)
                            {
                                element.Outputs.Add(output);
                            }
                        }

                        break;

                    case "source":
                        var lines = reader.ReadArrayOrStringAsString();

                        if (lines is { })
                        {
                            element.Contents = lines;

                            if (lines.StartsWith("#!"))
                            {
                                var i = lines.IndexOfAny(new[] { ' ', '\n', '\r' });

                                if (i > 0)
                                {
                                    inferredTargetKernelName = lines[2..i];
                                }
                            }
                        }

                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (inferredTargetKernelName is { })
                {
                    element.InferredTargetKernelName = inferredTargetKernelName;
                }

                return element;
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(Utf8JsonWriter writer, InteractiveDocumentElement element, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("cell_type");

        if (element.Language == "markdown")
        {
            writer.WriteStringValue("markdown");
        }
        else
        {
            writer.WriteStringValue("code");

            writer.WritePropertyName("execution_count");
            writer.WriteNumberValue(element.ExecutionCount);
        }

        if (element.Id is { })
        {
            writer.WritePropertyName("id");
            writer.WriteStringValue(element.Id);
        }

        if (element.Metadata is null)
        {
            element.Metadata = new Dictionary<string, object>();
        }

        if (element.Language is not null &&
            element.Language != "markdown")
        {
            element.Metadata.GetOrAdd("dotnet_interactive",
                                      _ => new Dictionary<string, object>())
                ["language"] = element.Language;
        }

        writer.WritePropertyName("metadata");
        JsonSerializer.Serialize(writer, element.Metadata, options);

        if (element.Language != "markdown")
        {
            writer.WritePropertyName("outputs");
            JsonSerializer.Serialize(writer, element.Outputs, options);
        }

        writer.WritePropertyName("source");
        var lines = element.Contents.SplitIntoLines().EnsureTrailingNewlinesOnAllButLast();
        JsonSerializer.Serialize(writer, lines, options);

        writer.WriteEndObject();
    }

}