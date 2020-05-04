// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Server
{
    public class FileSystemInfoJsonConverter : JsonConverter<FileSystemInfo>
    {
        public override void WriteJson(JsonWriter writer, FileSystemInfo value, JsonSerializer serializer)
        {
            writer.WriteValue(value.FullName);
        }

        public override FileSystemInfo ReadJson(JsonReader reader, Type objectType, FileSystemInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value is string path)
            {
                if (objectType == typeof(FileInfo))
                {
                    return new FileInfo(path);
                }

                if (objectType == typeof(DirectoryInfo))
                {
                    return new DirectoryInfo(path);
                }
            }

            return null;
        }

        public override bool CanRead { get; } = true;

        public override bool CanWrite { get; }= true;
    }
}