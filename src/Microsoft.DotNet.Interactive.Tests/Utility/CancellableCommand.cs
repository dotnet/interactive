// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class CancellableCommand : KernelCommand
    {
        public CancellableCommand(string targetKernelName = null, KernelCommand parent = null) : base(targetKernelName, parent)
        {
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            HasRun = true;

            while (!context.CancellationToken.IsCancellationRequested)
            {

            }

            HasBeenCancelled = true;

            return Task.CompletedTask;
        }

        public bool HasBeenCancelled { get; private set; }

        public bool HasRun { get; private set; }
    }
}