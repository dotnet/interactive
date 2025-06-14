// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Commands;

public class DisplayValue : KernelCommand
{
    public DisplayValue(FormattedValue formattedValue, string valueId = null)
    {
        FormattedValue = formattedValue;
        ValueId = valueId;
    }

    public FormattedValue FormattedValue { get; }

    public string ValueId { get; }

    public override Task InvokeAsync(KernelInvocationContext context)
    {
        context.Publish(
            new DisplayedValueProduced(
                null,
                this,
                formattedValues: new[] { FormattedValue },
                valueId: ValueId));

        return Task.CompletedTask;
    }
}