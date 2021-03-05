// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Notebook;

namespace Microsoft.DotNet.Interactive.Server
{
    internal static class Serializer
    {
        static Serializer()
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            JsonSerializerOptions.Converters.Add(new DataDictionaryConverter());
            JsonSerializerOptions.Converters.Add(new NotebookCellOutputConverter());
        }

        public static JsonSerializerOptions JsonSerializerOptions { get; }
    }
}
