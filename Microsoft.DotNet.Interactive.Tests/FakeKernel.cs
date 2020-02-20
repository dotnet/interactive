// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class FakeKernel : KernelBase
    {
        public FakeKernel([CallerMemberName] string name = null)
        {
            Name = name;
        }

        public KernelCommandInvocation Handle { get; set; }

        protected override Task HandleSubmitCode(SubmitCode command, KernelInvocationContext context)
        {
            Handle(command, context);
            return Task.CompletedTask;
        }

        protected override Task HandleRequestCompletion(RequestCompletion command, KernelInvocationContext context)
        {
            Handle(command, context);
            return Task.CompletedTask;
        }
    }
}