// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.App;

internal class RecentConnectionListConverter : JsonConverter<RecentConnectionList>
{
    public override RecentConnectionList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        int? capacity = null;
        List<ConnectionShortcut> items = new();

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "capacity":
                        if (reader.Read() && reader.TokenType is JsonTokenType.Number)
                        {
                            capacity = reader.GetInt32();
                        }

                        break;

                    case "items":

                        if (reader.ReadArray<ConnectionShortcut>(options) is { } shortcuts)
                        {
                            items.AddRange(shortcuts);
                        }

                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        var list = new RecentConnectionList
        {
            Capacity = capacity ?? RecentConnectionList.DefaultCapacity
        };

        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    public override void Write(
        Utf8JsonWriter writer,
        RecentConnectionList list,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("capacity", list.Capacity);

        writer.WritePropertyName("items");
        writer.WriteStartArray();

        foreach (var shortcut in list)
        {
            JsonSerializer.Serialize(writer, shortcut, options);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}