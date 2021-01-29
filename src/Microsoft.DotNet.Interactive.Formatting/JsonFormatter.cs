// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class JsonFormatter
    {
        static JsonFormatter()
        {
            SerializerOptions = new JsonSerializerOptions{
                WriteIndented = false,
                IgnoreNullValues = true,
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

    }
}