// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging;

public static class MessageFormatter
{
    static MessageFormatter()
    {
        SerializerOptions = new JsonSerializerOptions(JsonFormatter.SerializerOptions)
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = null,
            Converters =
            {
                new MessageConverter()
            }
        };
    }

    public static JsonSerializerOptions SerializerOptions { get; }
}