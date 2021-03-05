﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class Cancel : KernelCommand
    {
        public Cancel(string targetKernelName = null): base(targetKernelName)
        {
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            return Task.CompletedTask;
        }
    }
}