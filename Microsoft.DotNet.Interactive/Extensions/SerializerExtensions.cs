// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public static class SerializerExtensions
    {
        public static T DeserializeFromString<T>(this JsonSerializer jsonSerializer, string text)
        {
            var reader = new StringReader(text);
            var result = (T)jsonSerializer.Deserialize(reader, typeof(T));
            return result;
        }

        public static string SerializeToString<T>(this JsonSerializer jsonSerializer, T value)
        {
            using var writer = new StringWriter();
            jsonSerializer.Serialize(writer, value);
            var json = writer.ToString();
            return json;
        }
    }
}
