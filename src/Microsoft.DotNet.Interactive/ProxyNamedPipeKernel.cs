// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive
{
    public class ProxyNamedPipeKernel : KernelBase
    {
        private NamedPipeKernelClient _client;

        public ProxyNamedPipeKernel(string name, string pipeName) : base(name)
        {
            _client = new NamedPipeKernelClient(this, pipeName);
        }

        public Task ConnectAsync()
        {
            return _client.ConnectAsync();
        }

        public bool IsConnected => _client.IsConnected;

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
