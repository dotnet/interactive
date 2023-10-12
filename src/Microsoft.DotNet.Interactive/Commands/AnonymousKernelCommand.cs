// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.Commands;

[DebuggerStepThrough]
internal class AnonymousKernelCommand : KernelCommand
{
    public AnonymousKernelCommand(
        KernelCommandInvocation handler,
        string targetKernelName = null)
        : base(targetKernelName)
    {
        Handler = handler;
        ShouldPublishCompletionEvent = false;
    }

    internal override bool IsHidden => true;
}