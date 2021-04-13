// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class JavaScriptKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        public const string DefaultKernelName = "javascript";

        public JavaScriptKernel() : base(DefaultKernelName)
        {
        }

        public Task HandleAsync(
            SubmitCode command,
            KernelInvocationContext context)
        {
            return FrontendEnvironment.ExecuteClientScript(command.Code, context);
        }

        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            TrySetHandler(command, context);

            if (CommandTypeIsRegistered(command.GetType()) && command.Handler is null)
            {
                command.Handler = (kernelCommand, invocationContext) => FrontendEnvironment.ForwardCommand(kernelCommand, invocationContext);
            }

            await command.InvokeAsync(context);
        }
    }
}
