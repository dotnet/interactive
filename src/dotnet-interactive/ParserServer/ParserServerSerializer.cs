// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Documents.Json;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

public static class ParserServerSerializer
{
    static ParserServerSerializer()
    {
        JsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new ByteArrayConverter(),
                new DataDictionaryConverter(),
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new InteractiveDocumentConverter(),
                new NotebookParseRequestConverter(),
                new NotebookParseResponseConverter(),
            }
        };
    }

    public static JsonSerializerOptions JsonSerializerOptions { get; }
}
