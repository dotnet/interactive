// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Newtonsoft.Json;
using Pocket;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    public class TestStdioClient : IDisposable
    {
        private readonly Process _process;
        private readonly TextReaderInputStream _input;
        private readonly TextWriterOutputStream _output;
        private readonly Subject<KernelEvent> _events = new Subject<KernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly TaskCompletionSource<bool> _ready = new TaskCompletionSource<bool>();

        public IObservable<KernelEvent> Events => _events;

        public TestStdioClient(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _input = new TextReaderInputStream(process.StandardOutput);
            _output = new TextWriterOutputStream(process.StandardInput);

            _disposables = new CompositeDisposable()
            {
                _input.Subscribe(HandleLine),
                _input,
                _events,
                _process
            };
        }

        public Task WaitForReady()
        {
            return _ready.Task;
        }

        private void HandleLine(string line)
        {
            try
            {
                var eventEnvelope = KernelEventEnvelope.Deserialize(line);
                if (eventEnvelope.Event is KernelReady)
                {
                    _ready.SetResult(true);
                }

                _events.OnNext(eventEnvelope.Event);
            }
            catch (JsonReaderException)
            {
            }
        }

        public void SubmitCommand(KernelCommand command)
        {
            var serialized = KernelCommandEnvelope.Serialize(command);
            _output.Write(serialized);
        }

        public void Dispose()
        {
            _process.Kill();
            _disposables.Dispose();
        }
    }
}
