// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInvocationContext : IAsyncDisposable
    {
        private static readonly AsyncLocal<KernelInvocationContext> _current = new AsyncLocal<KernelInvocationContext>();

        private readonly ReplaySubject<KernelEvent> _events = new ReplaySubject<KernelEvent>();

        private readonly HashSet<KernelCommand> _childCommands = new HashSet<KernelCommand>();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private readonly List<Func<KernelInvocationContext, Task>> _onCompleteActions = new List<Func<KernelInvocationContext, Task>>();

        private readonly CancellationTokenSource _cancellationTokenSource;

        private KernelInvocationContext(KernelCommand command)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Command = command;
            CommandToSignalCompletion = command;
            Result = new KernelCommandResult(_events);

            _disposables.Add(_cancellationTokenSource);
            _disposables.Add(ConsoleOutput.Subscribe(c =>
                {
                    return new CompositeDisposable
                    {
                        c.Out.Subscribe(s => this.DisplayStandardOut(s, command)),
                        c.Error.Subscribe(s => this.DisplayStandardError(s, command))
                    };
                }));
        }

        public KernelCommand Command { get; }

        public bool IsComplete { get; private set; }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        internal KernelCommand CommandToSignalCompletion { get; set; }

        public void Complete(KernelCommand command)
        {
            if (command == Command)
            {
                Publish(new CommandSucceeded(Command));
                if (!_events.IsDisposed)
                {
                    _events.OnCompleted();
                }
                IsComplete = true;
            }
            else
            {
                _childCommands.Remove(command);
            }
        }

        public void Fail(
            Exception exception = null,
            string message = null)
        {
            Publish(new CommandFailed(exception, Command, message));

            _events.OnCompleted();
            _cancellationTokenSource.Cancel(false);
            IsComplete = true;
        }

        public void OnComplete(Func<KernelInvocationContext, Task> onComplete)
        {
            _onCompleteActions.Add(onComplete);
        }

        public void Publish(KernelEvent @event)
        {
            if (IsComplete)
            {
                return;
            }

            var command = @event.Command;

            if (command == null ||
                Command == command ||
                _childCommands.Contains(command))
            {
                _events.OnNext(@event);
            }
        }

        public IObservable<KernelEvent> KernelEvents => _events;

        public KernelCommandResult Result { get; }

        public static KernelInvocationContext Establish(KernelCommand command)
        {
            if (_current.Value == null || _current.Value.IsComplete)
            {
                var context = new KernelInvocationContext(command);

                _current.Value = context;
            }
            else
            {
                _current.Value._childCommands.Add(command);
            }

            return _current.Value;
        }

        public static KernelInvocationContext Current => _current.Value;

        public Kernel HandlingKernel { get; internal set; }

        public async Task QueueAction(
            KernelCommandInvocation action)
        {
            var command = new AnonymousKernelCommand(action);

            await HandlingKernel.SendAsync(command);
        }

        public ValueTask DisposeAsync()
        {
            if (_current.Value is { } active)
            {
                _current.Value = null;

                if (_onCompleteActions.Count > 0)
                {
                    Task.Run(async () =>
                        {
                            foreach (var action in _onCompleteActions)
                            {
                                await action.Invoke(this);
                            }
                        })
                        .Wait();
                }

                active.Complete(Command);

                _disposables.Dispose();
            }

            // This method is not async because it would prevent the setting of _current.Value to null from flowing up to the caller.
            return new ValueTask(Task.CompletedTask);
        }
    }
}