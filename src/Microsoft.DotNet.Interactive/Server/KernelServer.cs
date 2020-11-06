// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Messages;
using Newtonsoft.Json;

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
                    await DeserializeAndSendMessage(line);
                }),
                _kernel.KernelMessages.Subscribe(WriteMessageToOutput),
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

        public Task WriteAsync(string text) => DeserializeAndSendMessage(text);

        public IObservable<string> Output => _output.OutputObservable; 

        private async Task DeserializeAndSendMessage(string line)
        {
            KernelChannelMessage kernelMessage;
            try
            {
                kernelMessage = KernelChannelMessage.Deserialize(line);
                await _kernel.ProcessMessageAsync(kernelMessage);
            }
            catch (JsonReaderException ex)
            {
                WriteEventToOutput(
                    new DiagnosticLogEntryProduced(
                        $"Error while parsing message: {ex.Message}\n{line}"));
            }
            catch (ArgumentException ex)
            {
                WriteEventToOutput(
                    new DiagnosticLogEntryProduced(
                        $"Error while processing message: {ex.Message}\n{line}"));
            }
        }

        private void WriteEventToOutput(KernelEvent kernelEvent)
        {
            WriteMessageToOutput(new EventKernelMessage(kernelEvent));
        }

        private void WriteMessageToOutput(KernelChannelMessage message)
        {
            if (message is EventKernelMessage eventMessage &&
                eventMessage.Event is ReturnValueProduced rvp
                && rvp.Value is DisplayedValue)
            {
                return;
            }

            var serialized = KernelChannelMessage.Serialize(message);

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
