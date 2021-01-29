// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public static class JsonElementExtensions
    {
        public static object ToObject(this JsonElement source)
        {
            return source.ValueKind switch
            {
                JsonValueKind.String => source.GetString(),
                JsonValueKind.False => false,
                JsonValueKind.True => true,
                JsonValueKind.Number => source.GetDouble(),
                JsonValueKind.Object => source.ToDictionary(),
                JsonValueKind.Array => source.ToArray(),
                _ => null
            };
        }

        public static IDictionary<string, object> ToDictionary(this JsonElement source)
        {
            var ret =   new Dictionary<string, object>();
            foreach (var value in source.EnumerateObject())
            {
                ret[value.Name] = value.Value.ToObject();
            }
            return ret;
        }

        public static object[] ToArray(this JsonElement source)
        {
            var ret = new List<object>();
            foreach (var value in source.EnumerateArray())
            {
                ret.Add(value.ToObject());
            }
            return ret.ToArray();
        }
    }
}