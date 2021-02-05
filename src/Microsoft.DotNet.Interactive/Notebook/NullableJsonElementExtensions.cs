// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Notebook
{
    internal static class NullableJsonElementExtensions
    {
        public static IEnumerable<JsonElement> EnumerateArray(this JsonElement? jsonElement)
        {
            return (jsonElement?.EnumerateArray() as IEnumerable<JsonElement>) ?? Array.Empty<JsonElement>();
        }

        public static string GetRawText(this JsonElement? jsonElement)
        {
            return jsonElement?.GetRawText() ?? "{}";
        }

        public static string GetString(this JsonElement? jsonElement)
        {
            return jsonElement?.GetString();
        }
    }
}
