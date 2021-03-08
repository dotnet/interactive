// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class TestCommand : KernelCommand
    {
        private readonly Func<KernelCommand, KernelInvocationContext, Task> _handler;

        public TestCommand(Func<KernelCommand, KernelInvocationContext, Task> handler, string targetKernelName = null, KernelCommand parent = null) : base(targetKernelName, parent)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            return _handler(this, context);
        }
    }
}