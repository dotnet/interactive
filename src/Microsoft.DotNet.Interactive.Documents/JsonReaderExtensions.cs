// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents;

internal static class JsonReaderExtensions
{
    internal static T[]? ReadArray<T>(
        this ref Utf8JsonReader reader,
        JsonSerializerOptions options)
    {
        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<T[]>(ref reader, options);
        }

        return default;
    }

    internal static IDictionary<string, object>? ReadDataDictionary(
        this ref Utf8JsonReader reader,
        JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<IDictionary<string, object>>(ref reader, options);

    internal static int? ReadInt32(this ref Utf8JsonReader reader)
    {
        if (reader.Read() && reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        return null;
    }

    internal static string? ReadString(this ref Utf8JsonReader reader)
    {
        if (reader.Read() && reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        return null;
    }
    
    internal static string? ReadArrayOrStringAsString(
        this ref Utf8JsonReader reader)
    {
        if (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    var lines = JsonSerializer.Deserialize<string[]>(ref reader);

                    return string.Join("", lines ?? Array.Empty<string>());

                case JsonTokenType.String:
                    return reader.GetString();
            }
        }

        return null;
    }
}