// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueProduced : KernelEvent
    {
        [JsonIgnore] 
        public object Value { get; }
        public IReadOnlyCollection<FormattedValue> FormattedValues { get; }

        public ValueProduced(object value,
            RequestValue command,
            IReadOnlyCollection<FormattedValue> formattedValues = null) : base(command)
        {
            Value = value;
            FormattedValues = formattedValues;
        }
    }
}