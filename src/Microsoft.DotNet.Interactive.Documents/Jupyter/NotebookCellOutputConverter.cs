// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

internal class NotebookCellOutputConverter : JsonConverter<InteractiveDocumentOutputElement>
{
    public override InteractiveDocumentOutputElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        IDictionary<string, object>? data = null;
        string? text = null;
        string? errorName = null;
        string? errorValue = null;
        IEnumerable<string>? stackTrace = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "data":
                        data = JsonSerializer.Deserialize<IDictionary<string, object>>(ref reader, options);
                        break;

                    case "text":
                        text = JsonSerializer.Deserialize<string>(ref reader, options);
                        break;

                    case "errorName":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            errorName = reader.GetString();
                        }
                        break;

                    case "errorValue":
                        if (reader.Read() && reader.TokenType == JsonTokenType.String)
                        {
                            errorValue = reader.GetString();
                        }
                        break;

                    case "stackTrace":
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                        {
                            stackTrace = JsonSerializer.Deserialize<string[]>(ref reader, options);
                        }
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (text is not null)
                {
                    return new TextElement(text);
                }

                if (data is not null)
                {
                    return new DisplayElement(data);
                }

                if (errorName is not null && errorValue is not null && stackTrace is not null)
                {
                    return new ErrorElement(errorValue, errorName, stackTrace.ToArray());
                }
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

                writer.WritePropertyName("data");
                writer.WriteStartObject();

                foreach (var kvp in displayElement.Data)
                {
                    writer.WritePropertyName(kvp.Key);

                    if (kvp.Key == "application/json")
                    {
                        // FIX: (Write) JSON
                    }
                    else
                    {
                        var value = kvp.Value;

                        var lines = value switch
                        {
                            IEnumerable<string> enumerable => enumerable,
                            string s => s.SplitIntoLines(),
                            IEnumerable<object> os => os.Select(o => o switch
                            {
                                string s => s,
                                _ => throw new ArgumentException($"Expected string but found {o.GetType()}")
                            }),
                            object o when o.GetType() == typeof(object) => new string[0],
                            _ => throw new ArgumentException($"Expected IEnumerable<string> but received {kvp.Value.GetType()}")
                        };

                        writer.WriteStartArray();

                        foreach (var line in lines)
                        {
                            writer.WriteStringValue(line);
                        }

                        writer.WriteEndArray();
                    }
                }

                writer.WriteEndObject();

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

                writer.WritePropertyName("output_type");
                writer.WriteStringValue("execute_result");

                break;

            case TextElement textElement:

                writer.WritePropertyName("name");
                writer.WriteStringValue(textElement.Name);

                writer.WritePropertyName("output_type");
                writer.WriteStringValue("stream");
                
                writer.WriteStartArray("text");

                foreach (var line in textElement.Text.SplitIntoLines())
                {
                    writer.WriteStringValue(line);
                }
                writer.WriteEndArray();

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }

        writer.WriteEndObject();
    }
}