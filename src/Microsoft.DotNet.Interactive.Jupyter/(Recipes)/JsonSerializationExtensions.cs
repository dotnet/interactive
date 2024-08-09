// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Recipes;

internal static class JsonSerializationExtensions
{
    public static string ToJson(this object source) =>
        JsonSerializer.Serialize(source, Microsoft.DotNet.Interactive.Connection.Serializer.JsonSerializerOptions);

    public static T FromJsonTo<T>(this string json) =>
        JsonSerializer.Deserialize<T>(json, Microsoft.DotNet.Interactive.Connection.Serializer.JsonSerializerOptions);
}