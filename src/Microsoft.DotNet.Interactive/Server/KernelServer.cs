// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    public class KernelServer : IDisposable
    {
        private readonly Kernel _kernel;
        private readonly IInputTextStream _input;
        private readonly IOutputTextStream _output;
        private readonly CompositeDisposable _disposables;

        public KernelServer(
            Kernel kernel,
            IInputTextStream input,
            IOutputTextStream output,
            DirectoryInfo workingDir)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            if (workingDir is null)
            {
                throw new ArgumentNullException(nameof(workingDir));
            }

            _disposables = new CompositeDisposable
            {
                _input.Subscribe(async line =>
                {
                    await DeserializeAndSendCommand(line);
                }),
                _kernel.KernelEvents.Subscribe(WriteEventToOutput),
                _input
            };

            Environment.CurrentDirectory = workingDir.FullName;
        }

        public void NotifyIsReady()
        {
            WriteEventToOutput(new KernelReady());
        }

        public bool IsStarted => _input.IsStarted;

        public IObservable<string> Input => _input;

        public Task WriteAsync(string text) => DeserializeAndSendCommand(text);

        public IObservable<string> Output => _output.OutputObservable; 

        private async Task DeserializeAndSendCommand(string line)
        {
            IKernelCommandEnvelope kernelCommandEnvelope;
            try
            {
                kernelCommandEnvelope = KernelCommandEnvelope.Deserialize(line);
            }
            catch (Exception ex)
            {
                WriteEventToOutput(
                    new DiagnosticLogEntryProduced(
                        $"Error while parsing command: {ex.Message}\n{line}", KernelCommand.None));
                
                return;
            }
            
            await _kernel.SendAsync(kernelCommandEnvelope.Command);
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

        public void Start()
        {
            if (_input is InputTextStream pollingInputTextStream)
            {
                pollingInputTextStream.Start();
            }
        }
    }
}
