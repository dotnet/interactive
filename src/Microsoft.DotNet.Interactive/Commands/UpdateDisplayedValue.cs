// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class UpdateDisplayedValue : KernelCommandBase
    {
        public UpdateDisplayedValue(object value, FormattedValue formattedValue, string valueId)
        {
            if (string.IsNullOrWhiteSpace(valueId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(valueId));
            }

            Value = value;
            FormattedValue = formattedValue;
            ValueId = valueId;
        }

        public object Value { get; }

        public FormattedValue FormattedValue { get; }
        
        public string ValueId { get; }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            context.Publish(
                new DisplayedValueUpdated(
                    Value,
                    valueId: ValueId,
                    command: this,
                    formattedValues: new[] { FormattedValue }
                ));

            return Task.CompletedTask;
        }
    }
}