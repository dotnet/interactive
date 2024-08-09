// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.Utility;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;

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
                            element.KernelName = "markdown";
                        }

                        break;

                    case "execution_count":
                        element.ExecutionOrder = reader.ReadInt32() ?? 0;
                        break;

                    case "id":
                        element.Id = reader.ReadString();
                        break;

                    case "metadata":
                        var metadata = reader.ReadDataDictionary(options);
                        element.Metadata ??= new Dictionary<string, object>();
                        element.Metadata.MergeWith(metadata);

                        var kernelName = GetMetadataStringValue(element.Metadata, "polyglot_notebook", "kernelName")
                                      ?? GetMetadataStringValue(element.Metadata, "dotnet_interactive", "language");
                        if (kernelName is not null)
                        {
                            element.KernelName = kernelName;
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

    private static string? GetMetadataStringValue(IDictionary<string, object>? dict, string name1, string name2)
    {
        if (dict?.TryGetValue(name1, out var dotnet_interactive) == true &&
            dotnet_interactive is IDictionary<string, object> dotnet_interactive_dict &&
            dotnet_interactive_dict.TryGetValue(name2, out var languageStuff) &&
            languageStuff is string value)
        {
            return value;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, InteractiveDocumentElement element, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("cell_type");

        if (element.KernelName == "markdown")
        {
            writer.WriteStringValue("markdown");
        }
        else
        {
            writer.WriteStringValue("code");

            writer.WritePropertyName("execution_count");
            if (element.ExecutionOrder > 0)
            {
                writer.WriteNumberValue(element.ExecutionOrder);
            }
            else
            {
                writer.WriteNullValue();
            }
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

        if (element.KernelName is not null &&
            element.KernelName != "markdown")
        {
            element.Metadata.GetOrAdd("dotnet_interactive",
                                      _ => new Dictionary<string, object>())
                ["language"] = element.KernelName;

            element.Metadata.GetOrAdd("polyglot_notebook",
                                      _ => new Dictionary<string, object>())
                ["kernelName"] = element.KernelName;
        }

        writer.WritePropertyName("metadata");
        JsonSerializer.Serialize(writer, element.Metadata, options);

        if (element.KernelName != "markdown")
        {
            writer.WritePropertyName("outputs");
            JsonSerializer.Serialize(writer, element.Outputs, options);
        }

        writer.WritePropertyName("source");
        var lines = element.Contents.SplitIntoJupyterFileArray();
        JsonSerializer.Serialize(writer, lines, options);

        writer.WriteEndObject();
    }

}