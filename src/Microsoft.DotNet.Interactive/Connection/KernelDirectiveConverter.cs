// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Connection;

public class KernelDirectiveConverter : JsonConverter<KernelDirective>
{
    public override void Write(Utf8JsonWriter writer, KernelDirective value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("name", value.Name);

        switch (value)
        {
            case KernelSpecifierDirective specifier:
                writer.WriteString("kind", "kernelSpecifier");
                writer.WriteString("kernelName", specifier.KernelName);
                break;
            case KernelActionDirective:
                writer.WriteString("kind", "action");
                break;
            default:
                throw new NotSupportedException();
        }

        writer.WriteEndObject();
    }

    public override KernelDirective Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader, typeToConvert);

        string? name = null;
        string? kernelName = null;
        string? kind = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                switch (reader.GetString())
                {
                    case "name":
                        reader.Read();
                        name = reader.GetString();
                        break;

                    case "kernelName":
                        reader.Read();
                        kernelName = reader.GetString();
                        break;

                    case "kind":
                        reader.Read();
                        kind = reader.GetString();
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

        if (name is null)
        {
            throw new JsonException($"Property 'name' is required to deserialize a {nameof(KernelDirective)}.");
        }

        switch (kind)
        {
            case "action":
                return new KernelActionDirective(name);

            case "kernelSpecifier":

                if (kernelName is null)
                {
                    throw new JsonException($"Property 'kernelName' is required to deserialize a {nameof(KernelSpecifierDirective)}.");
                }

                return new KernelSpecifierDirective(name, kernelName);

            default:
                throw new NotSupportedException();
        }
    }
}