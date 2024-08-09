// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol;

internal class HistoryReplyConverter : JsonConverter<HistoryReply>
{
    public override HistoryReply Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.StartObject)
        {
            if (reader.Read() && reader.TokenType is JsonTokenType.PropertyName)
            {
                if (reader.GetString() is "history")
                {
                    if (reader.Read())
                    {
                        if (reader.TokenType is JsonTokenType.StartArray)
                        {
                            if (reader.Read())
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.StartArray:
                                        var elements = ReadHistoryElements(ref reader);
                                        if (!reader.Read() || reader.TokenType is not JsonTokenType.EndObject)
                                        {
                                            throw new JsonException();
                                        }

                                        return new HistoryReply(elements);
                                    case JsonTokenType.EndArray:
                                        if (reader.Read() && reader.TokenType is JsonTokenType.EndObject)
                                        {
                                            return new HistoryReply();
                                        }
                                        else
                                        {
                                            throw new JsonException();
                                        }
                                }
                            }

                        }
                        else if (reader.TokenType is JsonTokenType.Null)
                        {
                            if (!reader.Read() || reader.TokenType is not JsonTokenType.EndObject)
                            {
                                throw new JsonException();
                            }
                            return new HistoryReply();
                        }
                    }
                }
            }
        }
        throw new JsonException();
    }

    private IReadOnlyList<HistoryElement> ReadHistoryElements(ref Utf8JsonReader reader)
    {
        var elements = new List<HistoryElement>();
        while (reader.TokenType is JsonTokenType.StartArray)
        {
            int session;
            if (reader.Read() && reader.TokenType is JsonTokenType.Number)
            {
                session = reader.GetInt32();
            }
            else
            {
                throw new JsonException();
            }

            int lineNumber;
            if (reader.Read() && reader.TokenType is JsonTokenType.Number)
            {
                lineNumber = reader.GetInt32();
            }
            else
            {
                throw new JsonException();
            }

            if (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                    {
                        var input = reader.GetString();
                        elements.Add(new InputHistoryElement(session, lineNumber, input));
                    }
                        break;
                    case JsonTokenType.StartArray:
                    {
                        string input = null;
                        string output = null;
                        if (reader.Read() && reader.TokenType is JsonTokenType.String)
                        {
                            input = reader.GetString();
                        }
                        else
                        {
                            throw new JsonException();
                        }


                        if (reader.Read() && reader.TokenType is JsonTokenType.String)
                        {
                            output = reader.GetString();
                        }
                        else
                        {
                            throw new JsonException();
                        }

                        if (!reader.Read() || reader.TokenType is not JsonTokenType.EndArray)
                        {
                            throw new JsonException();
                        }

                        elements.Add(new InputOutputHistoryElement(session, lineNumber, input, output));
                    }
                        break;
                    default:
                        throw new JsonException();

                }

                if (reader.Read() && reader.TokenType is JsonTokenType.EndArray)
                {
                    reader.Read();
                }
            }
        }


        return elements;
    }

    public override void Write(Utf8JsonWriter writer, HistoryReply value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("history");

        foreach (var element in value.History)
        {
            switch (element)
            {
                case InputOutputHistoryElement inputOutputHistoryElement:
                    Write(writer, inputOutputHistoryElement);
                    break;
                case InputHistoryElement inputHistoryElement:
                    Write(writer, inputHistoryElement);
                    break;
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Write(Utf8JsonWriter writer, InputHistoryElement inputHistoryElement)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(inputHistoryElement.Session);
        writer.WriteNumberValue(inputHistoryElement.LineNumber);
        writer.WriteStringValue(inputHistoryElement.Input);
        writer.WriteEndArray();
    }

    private static void Write(Utf8JsonWriter writer, InputOutputHistoryElement inputOutputHistoryElement)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(inputOutputHistoryElement.Session);
        writer.WriteNumberValue(inputOutputHistoryElement.LineNumber);
        writer.WriteStartArray();
        writer.WriteStringValue(inputOutputHistoryElement.Input);
        writer.WriteStringValue(inputOutputHistoryElement.Output);
        writer.WriteEndArray();
        writer.WriteEndArray();
    }
}