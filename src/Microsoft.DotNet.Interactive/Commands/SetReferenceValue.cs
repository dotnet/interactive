// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    [NotSerializable("This command is cannot be serialized, consider using SetFormattedValue instead")]
    public class SetReferenceValue : KernelCommand
    {
        public object Value { get; }
        public string Name { get; }

        public SetReferenceValue(object value, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Name = name;
        }
    }
}