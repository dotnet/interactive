// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    internal static class ParserServerSerializer
    {
        static ParserServerSerializer()
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            JsonSerializerOptions.Converters.Add(new ByteArrayConverter());
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            JsonSerializerOptions.Converters.Add(new NotebookCellOutputConverter());
            JsonSerializerOptions.Converters.Add(new NotebookParseRequestConverter());
            JsonSerializerOptions.Converters.Add(new NotebookParseResponseConverter());
        }

        public static JsonSerializerOptions JsonSerializerOptions { get; }
    }
}
