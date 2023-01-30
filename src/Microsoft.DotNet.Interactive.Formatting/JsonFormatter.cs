// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting;

public static class JsonFormatter
{
    public const string MimeType = "application/json";

    static JsonFormatter()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new DataDictionaryConverter()
            }
        };
    }

    public static ITypeFormatter GetPreferredFormatterFor(Type type)
    {
        return Formatter.GetPreferredFormatterFor(type, MimeType);
    }

    internal static ITypeFormatter[] DefaultFormatters { get; } =
    {
        new JsonFormatter<string>((s, context) =>
        {
            var data = JsonSerializer.Serialize(s, SerializerOptions);
            context.Writer.Write(data);
            return true;
        }),
        new JsonFormatter<object>()
    };

    public static JsonSerializerOptions SerializerOptions { get; }
}