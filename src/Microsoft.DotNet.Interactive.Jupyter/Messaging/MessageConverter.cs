// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging;

internal class MessageConverter : JsonConverter<Message>
{
    public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        _ = JsonDocument.TryParseValue(ref reader, out JsonDocument doc);
        JsonElement root = doc.RootElement;

        _ = root.TryGetProperty("header", out JsonElement headerElement);
        _ = root.TryGetProperty("parent_header", out JsonElement parentHeaderElement);
        _ = root.TryGetProperty("metadata", out JsonElement metadataElement);
        _ = root.TryGetProperty("content", out JsonElement contentElement);

        return MessageExtensions.DeserializeMessage(
            signature: null,
            headerJson: headerElement.GetRawText(),
            parentHeaderJson: parentHeaderElement.GetRawText(),
            metadataJson: metadataElement.GetRawText(),
            contentJson: contentElement.GetRawText(),
            identifiers: null,
            options);
    }

    public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        Write(writer, value.Header, "header", options);
        Write(writer, value.ParentHeader ?? new object(), "parent_header", options);
        Write(writer, value.MetaData ?? new object(), "metadata", options);
        Write(writer, value.Content, "content", options);
        Write(writer, value.Buffers, "buffers", options);
        Write(writer, value.Channel, "channel", options);

        writer.WriteEndObject();
    }

    private void Write<T>(Utf8JsonWriter writer, T value, string propertyName, JsonSerializerOptions options)
    {
        var localOptions = new JsonSerializerOptions(options);
        localOptions.Converters.Remove(this);
        writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName);
        JsonSerializer.Serialize(writer, value, value.GetType(), localOptions);
    }
}