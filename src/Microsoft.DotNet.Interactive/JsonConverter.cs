// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive;

public abstract class JsonConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
{
    // FIX: (JsonConverter) can this be deleted and use Microsoft.DotNet.Interactive.Formatting instead?
    protected void EnsureStartObject(Utf8JsonReader reader, Type typeToConvert)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(
                $"Cannot deserialize {typeToConvert.Name}, expecting {JsonTokenType.StartObject} but found {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        OnWrite(writer, value, options);
    }

    protected virtual void OnWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var localOptions = new JsonSerializerOptions(options);
        localOptions.Converters.Remove(this);
        JsonSerializer.Serialize(writer, value, value.GetType(), localOptions);
    }
}