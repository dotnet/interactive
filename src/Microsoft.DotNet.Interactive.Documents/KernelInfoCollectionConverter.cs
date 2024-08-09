// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Documents;

internal class KernelInfoCollectionConverter : JsonConverter<KernelInfoCollection>
{
    public override KernelInfoCollection Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        var collection = new KernelInfoCollection();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "items":
                        var kernelInfos = reader.ReadArray<KernelInfo>(options);

                        if (kernelInfos is not null)
                        {
                            collection.AddRange(kernelInfos);
                        }

                        break;

                    case "defaultKernelName":
                        collection.DefaultKernelName = reader.ReadString();

                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
        }

        return collection;
    }

    public override void Write(
        Utf8JsonWriter writer,
        KernelInfoCollection collection,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("defaultKernelName");
        writer.WriteStringValue(collection.DefaultKernelName);

        writer.WritePropertyName("items");

        writer.WriteStartArray();

        foreach (var item in collection)
        {
            JsonSerializer.Serialize(writer, item, options);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}