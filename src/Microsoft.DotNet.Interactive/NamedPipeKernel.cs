// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive
{
    internal class NamedPipeKernel : ProxyKernel
    {
        private string _pipeName;
        private NamedPipeClientStream _clientStream;

        public NamedPipeKernel(string name) : base(name)
        {
        }

        private async Task PollEvents(string commandToken)
        {
            do
            {
                var message = await _clientStream.ReadMessageAsync();
                var kernelEvent = KernelEventEnvelope.Deserialize(message).Event;
                PublishEvent(kernelEvent);
                if (kernelEvent is CommandHandled || kernelEvent is CommandFailed)
                {
                    if (kernelEvent.Command.GetToken() == commandToken)
                    {
                        break;
                    }
                }
            } while (true);
        }

        protected override Task HandleRequestCompletion(RequestCompletion command, KernelInvocationContext context)
        {
            var envelope = KernelCommandEnvelope.Create(command);
            _clientStream.WriteMessage(KernelCommandEnvelope.Serialize(envelope));
            await _clientStream.FlushAsync();
            await PollEvents(envelope.Token);
        }

        protected async override Task HandleSubmitCode(SubmitCode command, KernelInvocationContext context)
        {
            var envelope = KernelCommandEnvelope.Create(command);
            _clientStream.WriteMessage(KernelCommandEnvelope.Serialize(envelope));
            await _clientStream.FlushAsync();
            await PollEvents(envelope.Token);
        }

        public string PipeName => _pipeName;

        public async Task ConnectAsync(string pipeName)
        {
            if (_clientStream != null)
            {
                _clientStream.Close();
            }

            _pipeName = pipeName;

            var clientStream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);
            await clientStream.ConnectAsync();
            clientStream.ReadMode = PipeTransmissionMode.Message;
            _clientStream = clientStream;
            RegisterForDisposal(clientStream);
        }
    }
}
