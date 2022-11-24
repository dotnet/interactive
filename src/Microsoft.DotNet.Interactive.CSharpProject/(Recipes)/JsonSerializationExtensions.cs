// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Recipes;

internal static class JsonSerializationExtensions
{
    public static string ToJson(this object source) =>
        JsonConvert.SerializeObject(source);

    public static T FromJsonTo<T>(this string json) =>
        JsonConvert.DeserializeObject<T>(json);
}