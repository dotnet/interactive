// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class CurrentVariable
    {
        public CurrentVariable(string name, Type type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public object Value { get; }

        public Type Type { get; }

        public string Name { get; }
    }
}
