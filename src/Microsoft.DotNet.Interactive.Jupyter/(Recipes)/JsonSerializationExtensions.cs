// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Formatting;

namespace Recipes
{
    internal static class JsonSerializationExtensions
    {
        static JsonSerializationExtensions()
        {
            SerializerOptions =
                new JsonSerializerOptions(JsonSerializerDefaults.General)
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            SerializerOptions.Converters.Add(new DataDictionaryConverter());
            SerializerOptions.Converters.Add(new BareObjectConverter());
        }

        public static JsonSerializerOptions SerializerOptions { get; }

        public static string ToJson(this object source) =>
            JsonSerializer.Serialize(source, SerializerOptions);

        public static T FromJsonTo<T>(this string json) =>
            JsonSerializer.Deserialize<T>(json, SerializerOptions);

        public static object FromJsonTo(this string json, Type type, JsonSerializerOptions options = null) =>
            JsonSerializer.Deserialize(json, type, options?? SerializerOptions);
    }
}
