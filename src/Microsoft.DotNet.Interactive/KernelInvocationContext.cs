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
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using CompositeDisposable = Pocket.CompositeDisposable;

namespace Microsoft.DotNet.Interactive;

public class KernelInvocationContext : IDisposable
{
    private static readonly AsyncLocal<KernelInvocationContext> _current = new();
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokenSources = new();

    private readonly ReplaySubject<KernelEvent> _events = new();

    private readonly ConcurrentDictionary<KernelCommand, ReplaySubject<KernelEvent>> _childCommands = new(KernelCommandTokenComparer.Instance);

    private readonly CompositeDisposable _disposables = new();

    private List<Action<KernelInvocationContext>> _onCompleteActions;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private bool _ownsCancellationTokenSource;

    private readonly int _consoleAsyncContextId;

    private KernelInvocationContext(KernelCommand command)
    {
        var operation = new OperationLogger(
            operationName: nameof(KernelInvocationContext),
            args: [command],
            category: nameof(KernelInvocationContext),
            logOnStart: true);

        _cancellationTokenSource =
            _cancellationTokenSources.GetOrAdd(
                command.GetOrCreateToken(),
                s =>
                {
                    _ownsCancellationTokenSource = true;
                    return new CancellationTokenSource();
                }
        );

        Command = command;

        Result = new KernelCommandResult(command);

        _disposables.Add(_events.Subscribe(Result.AddEvent));

        _disposables.Add(ConsoleOutput.Subscribe(c =>
        {
            return new CompositeDisposable
            {
                c.Out.Subscribe(s => this.DisplayStandardOut(s, command)),
                c.Error.Subscribe(s => this.DisplayStandardError(s, command))
            };
        }));

        if (AsyncContext.Id is not null)
        {
            _consoleAsyncContextId = AsyncContext.Id.Value;
        }

        _disposables.Add(operation);
    }

    internal bool IsFailed { get; private set; }

    public KernelCommand Command { get; }

    internal KernelCommand CurrentlyExecutingCommand
    {
        get
        {
            if (HandlingKernel?.Scheduler?.CurrentValue is { } command)
            {
                return command;
            }

            return Command;
        }
    }

    public bool IsComplete { get; private set; }

    public CancellationToken CancellationToken => _cancellationTokenSource.IsCancellationRequested
        ? new CancellationToken(true)
        : _cancellationTokenSource.Token;

    public void Complete(KernelCommand command)
    {
        SucceedOrFail(!IsFailed, command);
    }

    public void Fail(
        KernelCommand command,
        Exception exception = null,
        string message = null)
    {
        SucceedOrFail(false, command, exception, message);
    }

    internal void Cancel()
    {
        if (!IsComplete)
        {
            TryCancel();
            Fail(
                Command,
                message: "Command cancelled.");
        }
    }

    private readonly object _lockObj = new();

    private void SucceedOrFail(
        bool succeed,
        KernelCommand command,
        Exception exception = null,
        string message = null)
    {
        lock (_lockObj)
        {
            if (IsComplete)
            {
                return;
            }

            var completingMainCommand = command.Equals(Command);

            if (succeed && !IsFailed)
            {
                if (completingMainCommand)
                {
                    Publish(new CommandSucceeded(Command));
                    StopPublishingMainCommandEvents();
                }
                else
                {
                    if (command.ShouldPublishCompletionEvent == true)
                    {
                        Publish(new CommandSucceeded(command));
                    }

                    StopPublishingChildCommandEvents();
                }
            }
            else
            {
                if (completingMainCommand || command.ShouldPublishCompletionEvent != true)
                {
                    Publish(new CommandFailed(exception, Command, message));

                    StopPublishingMainCommandEvents();

                    TryCancel();

                    IsFailed = true;
                }
                else
                {
                    if (message is not null)
                    {
                        if (command.IsSelfOrDescendantOf(Command))
                        {
                            Publish(new ErrorProduced(message, command), publishOnAmbientContextOnly: true);
                        }
                    }

                    Publish(new CommandFailed(exception, command, message));

                    StopPublishingChildCommandEvents();
                }
            }

            if (completingMainCommand)
            {
                IsComplete = true;
            }
        }

        void StopPublishingMainCommandEvents()
        {
            if (!_events.IsDisposed)
            {
                _events.OnCompleted();
            }
        }

        void StopPublishingChildCommandEvents()
        {
            if (TryGetChildCommandEvents(command, out var events) &&
                !events.IsDisposed)
            {
                events.OnCompleted();
            }
        }
    }

    private void TryCancel()
    {
        if (!IsComplete)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }

    public void OnComplete(Action<KernelInvocationContext> onComplete)
    {
        if (_onCompleteActions is null)
        {
            _onCompleteActions = new();
        }
        _onCompleteActions.Add(onComplete);
    }

    public void Publish(KernelEvent @event)
    {
        Publish(@event, false);
    }

    public void Publish(KernelEvent @event, bool publishOnAmbientContextOnly)
    {
        if (IsComplete)
        {
            return;
        }

        var command = @event.Command;

        if (HandlingKernel is not null)
        {
            @event.StampRoutingSlipAndLog(HandlingKernel.KernelInfo.Uri);
        }

        if (!publishOnAmbientContextOnly && TryGetChildCommandEvents(command, out var events))
        {
            events.OnNext(@event);
        }
        else if (Command.Equals(command))
        {
            _events.OnNext(@event);
        }
        else
        {
            if (command.IsSelfOrDescendantOf(Command))
            {
                _events.OnNext(@event);
            }
            else if (command.HasSameRootCommandAs(Command))
            {
                _events.OnNext(@event);
            }
        }
    }

    public IObservable<KernelEvent> KernelEvents => _events;

    public KernelCommandResult Result { get; }

    private bool TryGetChildCommandEvents(KernelCommand command, out ReplaySubject<KernelEvent> events)
    {
        return _childCommands.TryGetValue(command, out events);
    }

    internal KernelCommandResult ResultFor(KernelCommand command)
    {
        if (command.Equals(Command))
        {
            return Result;
        }
        else
        {
            TryGetChildCommandEvents(command, out var events);

            var result = new KernelCommandResult(command);

            using var _ = events.Subscribe(result.AddEvent);

            return result;
        }
    }

    public static KernelInvocationContext GetOrCreateAmbientContext(KernelCommand command, ConcurrentDictionary<string, KernelInvocationContext> contextsByRootToken = null)
    {
        if (_current.Value is null)
        {
            if (contextsByRootToken is null)
            {
                _current.Value = new KernelInvocationContext(command);
            }
            else
            {
                var rootToken = KernelCommand.GetRootToken(command.GetOrCreateToken());

                if (contextsByRootToken.TryGetValue(rootToken, out var rootContext))
                {
                    _current.Value = rootContext;
                    AddChildCommandToContext(command, rootContext);
                    var consoleSubscription = ConsoleOutput.InitializeFromAsyncContext(rootContext._consoleAsyncContextId);
                    rootContext._disposables.Add(consoleSubscription);
                }
                else
                {
                    _current.Value = new KernelInvocationContext(command);
                    contextsByRootToken.TryAdd(rootToken, _current.Value);
                    _current.Value.OnComplete(c =>
                    {
                        contextsByRootToken.TryRemove(rootToken, out _);
                    });
                }
            }
        }
        else if (_current.Value.IsComplete)
        {
            _current.Value = new KernelInvocationContext(command);
        }
        else if (!ReferenceEquals(_current.Value.Command, command))
        {
            var currentContext = _current.Value;

            AddChildCommandToContext(command, currentContext);
        }

        return _current.Value;

        static void AddChildCommandToContext(KernelCommand childCommand, KernelInvocationContext currentContext)
        {
            if (childCommand.Parent is null)
            {
                childCommand.SetParent(currentContext.Command);
            }

            currentContext._childCommands.GetOrAdd(childCommand, innerCommand =>
            {
                var replaySubject = new ReplaySubject<KernelEvent>();

                var subscription = replaySubject
                    .Where(e =>
                    {
                        if (innerCommand.OriginUri is not null)
                        {
                            // if executing on behalf of a proxy, don't swallow anything
                            return true;
                        }

                        return e is not CommandSucceeded and not CommandFailed;
                    })
                    .Subscribe(e => currentContext._events.OnNext(e));

                currentContext._disposables.Add(subscription);

                return replaySubject;
            });
        }
    }

    public static KernelInvocationContext Current => _current.Value;

    public Kernel HandlingKernel { get; internal set; }

    public void Dispose()
    {
        if (_current.Value == this)
        {
            _current.Value = null;
        }

        if (_onCompleteActions?.Count > 0)
        {
            foreach (var action in _onCompleteActions)
            {
                action.Invoke(this);
            }

            _onCompleteActions.Clear();
        }

        Complete(Command);

        if (_ownsCancellationTokenSource)
        {
            _cancellationTokenSources.TryRemove(Command.GetOrCreateToken(), out _);
            _cancellationTokenSource.Dispose();
        }
        _disposables.Dispose();
    }

    internal void CancelWithSuccess()
    {
        Complete(Command);
        TryCancel();
    }

    internal DirectiveNode CurrentlyParsingDirectiveNode { get; set; }

    internal class KernelCommandTokenComparer : IEqualityComparer<KernelCommand>
    {
        private KernelCommandTokenComparer()
        {
        }

        public static readonly KernelCommandTokenComparer Instance = new();

        public bool Equals(KernelCommand x, KernelCommand y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is not null && y is not null)
            {
                return x.GetOrCreateToken() == y.GetOrCreateToken();
            }

            return false;
        }

        public int GetHashCode(KernelCommand obj)
        {
            return obj.GetOrCreateToken().GetHashCode();
        }
    }
}
