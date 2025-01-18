// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public abstract class KernelDirectiveCommand : KernelCommand
{
    public virtual IEnumerable<string> GetValidationErrors(CompositeKernel kernel) => [];

    internal DirectiveNode DirectiveNode { get; set; }
}