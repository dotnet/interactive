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

namespace Microsoft.DotNet.Interactive
{
    public class KernelInvocationContext : IAsyncDisposable
    {
        private static readonly AsyncLocal<KernelInvocationContext> _current = new AsyncLocal<KernelInvocationContext>();

        private readonly ReplaySubject<IKernelEvent> _events = new ReplaySubject<IKernelEvent>();

        private readonly HashSet<IKernelCommand> _childCommands = new HashSet<IKernelCommand>();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private readonly List<Func<KernelInvocationContext, Task>> _onCompleteActions = new List<Func<KernelInvocationContext, Task>>();
        private FrontendEnvironmentBase _frontendEnvironment;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private KernelInvocationContext(IKernelCommand command)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Command = command;
            Result = new KernelCommandResult(_events);
        }

        public IKernelCommand Command { get; }

        public bool IsComplete { get; private set; }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void Complete(IKernelCommand command)
        {
            if (command == Command)
            {
                Publish(new CommandHandled(Command));
                if (!_events.IsDisposed)
                {
                    _events.OnCompleted();
                }
                IsComplete = true;
                _cancellationTokenSource.Cancel();
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
            IsComplete = true;
            _cancellationTokenSource.Cancel();
        }

        public void OnComplete(Func<KernelInvocationContext, Task> onComplete)
        {
            _onCompleteActions.Add(onComplete);
        }

        public void Publish(IKernelEvent @event)
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

        public IObservable<IKernelEvent> KernelEvents => _events;

        public IKernelCommandResult Result { get; }

        public static KernelInvocationContext Establish(IKernelCommand command)
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

        public IKernel HandlingKernel { get; set; }

        public IKernel CurrentKernel { get; set; }

        public async Task QueueAction(
            KernelCommandInvocation action)
        {
            var command = new AnonymousKernelCommand(action);

            await HandlingKernel.SendAsync(command);
        }

        public ValueTask DisposeAsync()
        {
            if (_current.Value is {} active)
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

        public FrontendEnvironmentBase FrontendEnvironment
        {
            get => _frontendEnvironment?? new AutomationEnvironment();
            internal set => _frontendEnvironment = value;
        }
    }
}