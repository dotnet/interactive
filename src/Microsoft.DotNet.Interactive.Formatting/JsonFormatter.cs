// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class JsonFormatter
    {
        static JsonFormatter()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.None
            };

            Formatter.Clearing += (sender, args) => DefaultFormatters = new DefaultJsonFormatterSet();
        }

        public const string MimeType = "application/json";

        internal static IFormatterSet DefaultFormatters { get; private set; } = new DefaultJsonFormatterSet();

        public static JsonSerializerSettings SerializerSettings { get; }

        public static ITypeFormatter Create(Type type)
        {
            var genericCreateForAllMembers = typeof(JsonFormatter<>)
                                             .MakeGenericType(type)
                                             .GetMethod(nameof(JsonFormatter<object>.Create));

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, null);
        }
    }
}