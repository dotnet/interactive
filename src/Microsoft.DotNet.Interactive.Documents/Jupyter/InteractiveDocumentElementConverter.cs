// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

internal class InteractiveDocumentElementConverter : JsonConverter<InteractiveDocumentElement>
{
    public override InteractiveDocumentElement Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        InteractiveDocumentElement element = new();
        string? possibleTargetKernelName = null;

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
                        var dict = reader.ReadDataDictionary(options);
                        element.Metadata = dict ?? new Dictionary<string, object>();

                        if (element.Metadata.TryGetValue("dotnet_interactive", out var dotnet_interactive) &&
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
                        var lines = reader.ReadArray<string>(options) ?? Array.Empty<string>();

                        element.Contents = string.Join("\n", lines);

                        possibleTargetKernelName = lines.Length > 0 && lines[0].StartsWith("#!")
                                                       ? lines[0].Substring(2)
                                                       : null;

                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (possibleTargetKernelName is { })
                {
                    element.Language = possibleTargetKernelName;
                }

                return element;
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(Utf8JsonWriter writer, InteractiveDocumentElement value, JsonSerializerOptions options)
    {
        base.Write(writer, value, options);
    }

    private static string[] GetTextLines(JsonElement? jsonElement)
    {
        var textLines = jsonElement?.ValueKind switch
        {
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(element => element.GetString()?.TrimNewline()).ToArray(),
            JsonValueKind.String => jsonElement.GetString()?.SplitIntoLines(),
            _ => null
        } ?? Array.Empty<string>();

        return textLines!;
    }

    private static string GetTextAsSingleString(JsonElement? jsonElement)
    {
        var textLines = GetTextLines(jsonElement);
        return string.Join("\n", textLines);
    }
}