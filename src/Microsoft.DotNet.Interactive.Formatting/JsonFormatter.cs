// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class JsonFormatter
    {
        static JsonFormatter()
        {
            SerializerOptions = new JsonSerializerOptions{
                WriteIndented = false,
                IgnoreNullValues = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public static ITypeFormatter GetPreferredFormatterFor(Type type)
        {
            return Formatter.GetPreferredFormatterFor(type, MimeType);
        }

        public static ITypeFormatter GetPreferredFormatterFor<T>() =>
            GetPreferredFormatterFor(typeof(T));

        public const string MimeType = "application/json";

        internal static ITypeFormatter[] DefaultFormatters { get; } = DefaultJsonFormatterSet.DefaultFormatters;

        public static JsonSerializerOptions SerializerOptions { get; }

        public static JsonString SerializeToJson<T>(this T source) =>
            new JsonString(JsonConvert.SerializeObject(source));

        public static IHtmlContent JsonEncode(this string source) =>
            new JsonString(JsonConvert.ToString(source));
    }
}