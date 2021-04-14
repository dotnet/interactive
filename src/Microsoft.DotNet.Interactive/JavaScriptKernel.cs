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

        protected override Func<TCommand, KernelInvocationContext, Task> CreateDefaultHandlerForCommandType<TCommand>()
        {
            return (kernelCommand, _) => ForwardCommand(kernelCommand);
        }

        private Task ForwardCommand(KernelCommand command)
        {
            return _client.SendAsync(command);
        }
    }
}
