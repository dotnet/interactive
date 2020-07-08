// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;

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
            Handler = CommandHandler.Create<KernelInvocationContext>(Handle);
        }

        public Kernel Kernel { get; }

        private void Handle(KernelInvocationContext context)
        {
            context.HandlingKernel = Kernel;
        }
    }
}