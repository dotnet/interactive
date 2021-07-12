// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public static class KernelExtensions
    {
        public static void UseVSCodeHelpers<TKernel>(this TKernel kernel, Kernel rootKernel) where TKernel : DotNetKernel
        {
            rootKernel.RegisterCommandType<GetInput>();
            KernelEventEnvelope.RegisterEvent<InputProduced>();

            var interactiveHost = new VSCodeInteractiveHost(rootKernel);
            var command = new AnonymousKernelCommand((_command, context) =>
            {
                return kernel.SetVariableAsync("InteractiveHost", interactiveHost, typeof(IInteractiveHost));
            });
            kernel.DeferCommand(command);
        }
    }
}
