// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueProduced : KernelEvent
    {
        [JsonIgnore] 
        public object Value { get; }

        public string Name { get; }
        public FormattedValue FormattedValue { get; }

        public ValueProduced(object value,
            string name,
            RequestValue command,
            FormattedValue formattedValue = null) : base(command)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Value = value;
            Name = name;
            FormattedValue = formattedValue;
        }
    }
}