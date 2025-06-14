// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Events;

public class StandardErrorValueProduced : DisplayEvent
{
    public StandardErrorValueProduced(
        KernelCommand command,
        IReadOnlyCollection<FormattedValue> formattedValues = null,
        string valueId = null) : base(null, command, formattedValues, valueId)
    {
    }
}