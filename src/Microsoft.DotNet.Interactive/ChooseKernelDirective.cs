// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.DotNet.Interactive
{
    public class ChooseKernelDirective : Command
    {
        public ChooseKernelDirective(Kernel kernel, string? description = null) : 
            base($"#!{kernel.Name}", 
                 description ?? $"Run the code that follows using the {kernel.Name} kernel.")
        {
            Kernel = kernel;
            Handler = CommandHandler.Create<KernelInvocationContext, InvocationContext>(Handle);
        }

        public Kernel Kernel { get; }

        protected virtual Task Handle(KernelInvocationContext kernelInvocationContext, InvocationContext commandLineInvocationContext)
        {
            kernelInvocationContext.HandlingKernel = Kernel;
            return Task.CompletedTask;
        }
    }
}