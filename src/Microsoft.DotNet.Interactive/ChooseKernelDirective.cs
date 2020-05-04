// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;

#nullable enable

namespace Microsoft.DotNet.Interactive
{
    internal class ChooseKernelDirective : Command
    {
        public ChooseKernelDirective(IKernel kernel) : 
            base($"#!{kernel.Name}", 
                 $"Run the code that follows using the {kernel.Name} kernel.")
        {
            Kernel = kernel;
            Handler = CommandHandler.Create<KernelInvocationContext>(Handle);
        }

        public IKernel Kernel { get; }

        private void Handle(KernelInvocationContext context)
        {
            context.HandlingKernel = Kernel;
        }
    }
}