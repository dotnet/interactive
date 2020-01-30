// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Server
{
    public class StandardIOKernelServer : IDisposable
    {
        private readonly IKernel _kernel;
        private readonly InputTextStream _input;
        private readonly OutputTextStream _output;
        private readonly CompositeDisposable _disposables;

        public StandardIOKernelServer(
            IKernel kernel, 
            TextReader input, 
            TextWriter output) : this(kernel, new InputTextStream(input), new OutputTextStream(output))
        {
        }

        private StandardIOKernelServer(
            IKernel kernel, 
            InputTextStream input,
            OutputTextStream output)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _disposables = new CompositeDisposable
            {
                _input.Subscribe(async line =>
                {
                    await DeserializeAndSendCommand(line);
                }),
                _kernel.KernelEvents.Subscribe(WriteEventToOutput)
            };
        }

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
                    new DiagnosticLogEventProduced(
                        $"Error while parsing command: {ex.Message}\n{line}"));
                
                return;
            }

            await _kernel.SendAsync(streamKernelCommand.Command);
        }

        private void WriteEventToOutput(IKernelEvent kernelEvent)
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
    }
}
