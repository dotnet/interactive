// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.Utility;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

internal class InteractiveDocumentOutputElementConverter : JsonConverter<InteractiveDocumentOutputElement>
{
    public override InteractiveDocumentOutputElement Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        IDictionary<string, object>? data = null;
        string? text = null;
        string? name = null;
        string? ename = null;
        string? errorValue = null;
        string? outputType = null;
        int? executionCount = null;
        IDictionary<string, object>? metadata = null;
        IEnumerable<string>? stackTrace = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "data":
                        data = reader.ReadDataDictionary(options);
                        break;

                    case "ename":
                        ename = reader.ReadString();
                        break;

                    case "evalue":
                        errorValue = reader.ReadString();
                        break;

                    case "execution_count":
                        executionCount = reader.ReadInt32();
                        break;

                    case "metadata":
                        metadata = reader.ReadDataDictionary(options);
                        break;

                    case "name":
                        name = reader.ReadString();
                        break;

                    case "output_type":
                        outputType = reader.ReadString();
                        break;

                    case "traceback":
                        stackTrace = reader.ReadArray<string>(options);
                        break;

                    case "text":
                        text = reader.ReadArrayOrStringAsString();
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                InteractiveDocumentOutputElement element;

                switch (outputType)
                {
                    case "display_data":
                        var displayElement = new DisplayElement(data ?? new Dictionary<string, object>());
                        if (metadata is not null)
                        {
                            displayElement.Metadata.MergeWith(metadata);
                        }
                        element = displayElement;

                        break;

                    case "error":
                        element = new ErrorElement(errorValue, ename, stackTrace?.ToArray());
                        break;

                    case "execute_result":
                        var returnValueElement = new ReturnValueElement
                        {
                            ExecutionOrder = executionCount ?? 0
                        };

                        if (data is not null)
                        {
                            returnValueElement.Data.MergeWith(data);
                        }

                        if (metadata is not null)
                        {
                            returnValueElement.Metadata.MergeWith(metadata);
                        }

                        element = returnValueElement;
                        break;

                    case "stream":
                        element = new TextElement(text, name);
                        break;

                    default:
                        throw new JsonException($"Unrecognized output_type: '{outputType}'");
                }

                return element;
            }
        }

        throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        InteractiveDocumentOutputElement element,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (element)
        {
            case DisplayElement displayElement:

                WriteData(displayElement);

                writer.WritePropertyName("metadata");
                JsonSerializer.Serialize(writer, displayElement.Metadata, options);

                writer.WritePropertyName("output_type");
                writer.WriteStringValue("display_data");
                break;

            case ErrorElement errorElement:
                writer.WritePropertyName("ename");
                writer.WriteStringValue(errorElement.ErrorName);

                writer.WritePropertyName("evalue");
                writer.WriteStringValue(errorElement.ErrorValue);

                writer.WritePropertyName("output_type");
                writer.WriteStringValue("error");

                writer.WriteStartArray("traceback");
                foreach (var line in errorElement.StackTrace)
                {
                    writer.WriteStringValue(line);
                }

                writer.WriteEndArray();

                break;

            case ReturnValueElement returnValueElement:

                WriteData(returnValueElement);

                writer.WritePropertyName("execution_count");
                writer.WriteNumberValue(returnValueElement.ExecutionOrder);

                writer.WritePropertyName("metadata");
                JsonSerializer.Serialize(writer, returnValueElement.Metadata, options);

                writer.WritePropertyName("output_type");
                writer.WriteStringValue("execute_result");

                break;

            case TextElement textElement:

                writer.WritePropertyName("name");
                writer.WriteStringValue(textElement.Name);

                writer.WritePropertyName("output_type");
                writer.WriteStringValue("stream");

                writer.WriteStartArray("text");
                var text = textElement.Text.SplitIntoJupyterFileArray();
                foreach (var line in text)
                {
                    writer.WriteStringValue(line);
                }

                writer.WriteEndArray();

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }

        writer.WriteEndObject();

        void WriteData(IDataElement displayElement)
        {
            writer.WritePropertyName("data");
            writer.WriteStartObject();

            foreach (var kvp in displayElement.Data)
            {
                writer.WritePropertyName(kvp.Key);

                var value = kvp.Value;

                var lines = value switch
                {
                    IEnumerable<string> enumerable => enumerable,
                    string s => s.SplitIntoJupyterFileArray(),
                    IEnumerable<object> os => os.Select(o => o switch
                    {
                        string s => s,
                        _ => throw new ArgumentException($"Expected string but found {o.GetType()}")
                    }),
                    { } o when o.GetType() == typeof(object) => Array.Empty<string>(),
                    _ => throw new ArgumentException($"Expected IEnumerable<string> but received {kvp.Value.GetType()}")
                };

                writer.WriteStartArray();

                foreach (var line in lines)
                {
                    writer.WriteStringValue(line);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}