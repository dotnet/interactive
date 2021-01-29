// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Server
{
    public class FileSystemInfoJsonConverter : JsonConverter<FileSystemInfo>
    {
        public override FileSystemInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.String)
            {
                var path = reader.GetString();
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                if (typeToConvert == typeof(FileInfo))
                {
                    return new FileInfo(path);
                }

                if (typeToConvert == typeof(DirectoryInfo))
                {
                    return new DirectoryInfo(path);
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, FileSystemInfo value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.FullName);
        }
    }
}