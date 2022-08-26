// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ValueSharing
{
    public class KernelValueInfo
    {
        private readonly IReadOnlyCollection<string> _preferredMimeTypes;

        public KernelValueInfo(string name, Type type = null)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public IReadOnlyCollection<string> PreferredMimeTypes
        {
            get => _preferredMimeTypes ??
                   (Type is not null
                        ? Formatter.GetPreferredMimeTypesFor(Type)
                        : Array.Empty<string>());
            init => _preferredMimeTypes = value;
        }

        [JsonIgnore] 
        public Type Type { get; }
    }
}