// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive
{
    public class JavaScriptKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        private KernelClientBase _client;
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

        public void SetKernelClient(KernelClientBase kernelClient)
        {
            _client = kernelClient ?? throw new ArgumentNullException(nameof(kernelClient));
        }

        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            TrySetHandler(command, context);

            if (_client is not null && IsCommandTypeRegistered(command.GetType()) && command.Handler is null)
            {
                command.Handler = (kernelCommand, _) => _client.SendAsync(kernelCommand); 
            }

            await command.InvokeAsync(context);
        }
    }
}
