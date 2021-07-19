// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using CompositeDisposable = Pocket.CompositeDisposable;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInvocationContext : IAsyncDisposable
    {
        private static readonly AsyncLocal<KernelInvocationContext> _current = new();

        private readonly ReplaySubject<KernelEvent> _events = new();

        private readonly HashSet<KernelCommand> _childCommands = new();

        private readonly CompositeDisposable _disposables = new();

        private readonly List<Func<KernelInvocationContext, Task>> _onCompleteActions = new();

        private readonly CancellationTokenSource _cancellationTokenSource;

        private KernelInvocationContext(KernelCommand command)
        {
            var operation = new OperationLogger(
                args: new object[] { ("KernelCommand", command) },
                exitArgs: () => new[] { ("KernelCommand", (object)command) },
                category: nameof(KernelInvocationContext),
                logOnStart: true);

            _cancellationTokenSource = new CancellationTokenSource();

            Command = command;

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

            _disposables.Add(operation);
        }

        public KernelCommand Command { get; }

        public bool IsComplete { get; private set; }

        public CancellationToken CancellationToken
        {
            get
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return new CancellationToken(true);
                }
                else
                {
                    return _cancellationTokenSource.Token;
                }
            }
        }

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
                Publish(new CommandSucceeded(command));
                _childCommands.Remove(command);
            }
        }

        public void Cancel()
        {
            if (!IsComplete)
            {
                TryCancel();
                Fail(new OperationCanceledException($"Command :{Command} cancelled."));
            }
        }

        public void Fail(
            Exception exception = null,
            string message = null)
        {
            if (!IsComplete)
            {
                Publish(new CommandFailed(exception, Command, message));
                _events.OnCompleted();

                TryCancel();

                IsComplete = true;
            }
        }

        private void TryCancel()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {

            }
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

            if (command is null ||
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
            if (_current.Value is null || _current.Value.IsComplete)
            {
                var context = new KernelInvocationContext(command);

                _current.Value = context;
            }
            else
            {
                if (_current.Value.Command != command)
                {
                    if (command.Parent is null)
                    {
                        command.Parent = _current.Value.Command;
                    }
                    _current.Value._childCommands.Add(command);
                }
            }

            return _current.Value;
        }

        public static KernelInvocationContext Current => _current.Value;

        public Kernel HandlingKernel { get; internal set; }

        public ValueTask DisposeAsync()
        {
            if (_current.Value is { } active)
            {
                if (_current.Value == this)
                {
                    _current.Value = null;
                }

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

        internal void CancelWithSuccess()
        {
            Complete(Command);
            TryCancel();
        }
    }
}