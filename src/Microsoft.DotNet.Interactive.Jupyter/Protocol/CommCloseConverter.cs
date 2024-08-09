// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol;

internal class CommCloseConverter : JsonConverter<CommClose>
{
    public override CommClose Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        _ = JsonDocument.TryParseValue(ref reader, out JsonDocument doc);
        JsonElement root = doc.RootElement;

        _ = root.TryGetProperty("comm_id", out JsonElement commIdElement);
        _ = root.TryGetProperty("data", out JsonElement dataElement);

        return new CommClose(commIdElement.GetString(),
            dataElement.ValueKind == JsonValueKind.Object ?
                dataElement.ToReadOnlyDictionary() : null);
    }

    public override void Write(Utf8JsonWriter writer, CommClose value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}