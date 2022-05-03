// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
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

        private readonly ConcurrentDictionary<KernelCommand, ReplaySubject<KernelEvent>> _childCommands = new (new CommandEqualityComparer());

        private readonly CompositeDisposable _disposables = new();

        private readonly List<Func<KernelInvocationContext, Task>> _onCompleteActions = new();

        private readonly CancellationTokenSource _cancellationTokenSource;

        private KernelInvocationContext(KernelCommand command)
        {
            var operation = new OperationLogger(
                operationName: command.ToString(),
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
            if (CommandEqualityComparer.Instance.Equals(command, Command))
            {
                Publish(new CommandSucceeded(command));
                if (!_events.IsDisposed)
                {
                    _events.OnCompleted();
                }
                IsComplete = true;
            }
            else
            {
                if (command.ShouldPublishCompletionEvent == true)
                {
                    Publish(new CommandSucceeded(command));
                }

                if (_childCommands.TryGetValue(command, out var events) && 
                    !events.IsDisposed)
                {
                    events.OnCompleted();
                }
            }
        }

        internal void Cancel()
        {
            if (!IsComplete)
            {
                TryCancel();
                Fail(
                    Command,
                    new OperationCanceledException($"Command :{Command} cancelled."));
            }
        }

        public void Fail(
            KernelCommand command,
            Exception exception = null,
            string message = null)
        {
            if (IsComplete)
            {
                return;
            }

            if (command is { ShouldPublishCompletionEvent: true } && 
                !CommandEqualityComparer.Instance.Equals(command, Command))
            {
                Publish(new CommandFailed(exception, command, message));

                if (_childCommands.TryGetValue(command, out var events) &&
                    !events.IsDisposed)
                {
                    events.OnCompleted();
                }
            }
            else
            {
                Publish(new CommandFailed(exception, Command, message));
                _events.OnCompleted();

                TryCancel();

                IsComplete = true;
            }
        }

        private void TryCancel()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
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

            if (_childCommands.TryGetValue(command, out var events))
            {
                events.OnNext(@event);
            }
            else if (CommandEqualityComparer.Instance.Equals(Command, command))
            {
                _events.OnNext(@event);
            }
            else if (string.Equals(Command.GetOrCreateToken(), command.GetOrCreateToken(), StringComparison.Ordinal))
            {
                // event from a sub-command that was remotely split
                _events.OnNext(@event);
            }
        }

        public IObservable<KernelEvent> KernelEvents => _events;

        public KernelCommandResult Result { get; }

        internal KernelCommandResult ResultFor(KernelCommand command)
        {
            if (CommandEqualityComparer.Instance.Equals(command, Command))
            {
                return Result;
            }
            else
            {
                var events = _childCommands[command];
                return new KernelCommandResult(events);
            }
        }

        public static KernelInvocationContext Establish(KernelCommand command)
        {
            if (_current.Value is null)
            {
                var context = new KernelInvocationContext(command);

                _current.Value = context;
            }
            else if (_current.Value.IsComplete)
            {
                // FIX: (Establish) 27 tests covering this... is it consistent?

                var context = new KernelInvocationContext(command);

                _current.Value = context;
            }
            else
            {
                if (!CommandEqualityComparer.Instance.Equals(_current.Value.Command, command))
                {
                    if (command.Parent is null)
                    {
                        command.Parent = _current.Value.Command;
                    }

                    _current.Value._childCommands.GetOrAdd(command, c =>
                    {
                        var replaySubject = new ReplaySubject<KernelEvent>();

                        var subscription = replaySubject
                                           .Where(e => e is not CommandSucceeded and not CommandFailed)
                                           .Subscribe(e => _current.Value._events.OnNext(e));

                        _current.Value._disposables.Add(subscription);
                        _current.Value._disposables.Add(replaySubject);

                        return replaySubject;
                    });
                }
                else
                {
                    // FIX: (Establish) 
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

        public Task ScheduleAsync(Func<KernelInvocationContext, Task> func) =>
            HandlingKernel.SendAsync(new AnonymousKernelCommand((_, invocationContext) =>
                                                                    func(invocationContext)));
    }
}