// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public static class JsonElementExtensions
    {
        public static JsonElement? GetPropertyFromPath(this JsonElement source, params string[] path)
        {
            var current = source;
            foreach (var propertyName in path)
            {
                if (!current.TryGetProperty(propertyName, out var propertyValue))
                {
                    return null;
                }

                current = propertyValue;

            }

            return current;
        }
    }
}
