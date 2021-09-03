// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueInfo
    {
        public ValueInfo(string name, Type clrValueType = null)
        {
            Name = name;
            ClrValueType = clrValueType;
        }

        public string Name { get; }

        [JsonIgnore]
        public Type ClrValueType { get; }
    }
}