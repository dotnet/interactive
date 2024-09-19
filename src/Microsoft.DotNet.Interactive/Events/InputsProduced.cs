// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class InputsProduced : KernelEvent
{
    public InputsProduced(Dictionary<string, string> values, RequestInputs command)
        : base(command)
    {
        Values = values;
    }

    public Dictionary<string, string> Values { get; }
}