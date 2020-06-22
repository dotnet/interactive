// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Pipes;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Server
{
    public class NamedPipeKernelServer : IDisposable
    {
        private readonly IKernel _kernel;
        private readonly NamedPipeServerStream _serverStream;
        private readonly InputPipeStream _input;
        private readonly OutputPipeStream _output;
        private readonly CompositeDisposable _disposables;

        private NamedPipeKernelServer(
            IKernel kernel,
            NamedPipeServerStream serverStream)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _serverStream = serverStream ?? throw new ArgumentNullException(nameof(serverStream));
            _input = new InputPipeStream(serverStream);
            _output = new OutputPipeStream(serverStream);

            _disposables = new CompositeDisposable
            {
                _input.Subscribe(async line =>
                {
                    await DeserializeAndSendCommand(line);
                }),
                _kernel.KernelEvents.Subscribe(WriteEventToOutput),
                serverStream
            };
        }

        public bool IsStarted => _input.IsStarted;

        public IObservable<string> Input => _input;

        public Task WriteAsync(string text) => DeserializeAndSendCommand(text);

        public IObservable<string> Output => _output.OutputObservable;

        private async Task DeserializeAndSendCommand(string line)
        {
            IKernelCommandEnvelope streamKernelCommand;
            try
            {
                streamKernelCommand = KernelCommandEnvelope.Deserialize(line);
            }
            catch (JsonReaderException ex)
            {
                WriteEventToOutput(
                    new DiagnosticLogEntryProduced(
                        $"Error while parsing command: {ex.Message}\n{line}"));

                return;
            }

            await _kernel.SendAsync(streamKernelCommand.Command);
        }

        private void WriteEventToOutput(KernelEvent kernelEvent)
        {
            if (kernelEvent is ReturnValueProduced rvp && rvp.Value is DisplayedValue)
            {
                return;
            }

            var envelope = KernelEventEnvelope.Create(kernelEvent);

            var serialized = KernelEventEnvelope.Serialize(envelope);

            _output.Write(serialized);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public static NamedPipeKernelServer WaitForConnection(IKernel kernel, string pipeName)
        {
            var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            serverStream.WaitForConnection();
            return new NamedPipeKernelServer(kernel, serverStream);
        }
    }
}
