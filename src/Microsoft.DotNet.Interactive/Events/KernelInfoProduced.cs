// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.Events;

[DebuggerStepThrough]
public class KernelInfoProduced : KernelEvent
{
    public KernelInfoProduced(
        KernelInfo kernelInfo,
        KernelCommand command) : base(command)
    {
        KernelInfo = kernelInfo;
    }

    public KernelInfo KernelInfo { get; }
}