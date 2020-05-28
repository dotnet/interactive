// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class DummyKernel : KernelBase
    {
        public DummyKernel(string name) : base(name)
        {
        }

        protected override Task HandleRequestCompletion(RequestCompletion command, KernelInvocationContext context)
        {
            return Task.CompletedTask;
        }

        protected override Task HandleSubmitCode(SubmitCode command, KernelInvocationContext context)
        {
            return Task.CompletedTask;
        }
    }
}
