// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive
{
    public class ValueInfo
    {
        public ValueInfo(string name, Type type = null)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        [JsonIgnore]
        public Type Type { get; }
    }
}