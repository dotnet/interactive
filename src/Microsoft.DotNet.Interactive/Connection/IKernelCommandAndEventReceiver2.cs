// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Connection;

public interface IKernelCommandAndEventReceiver2 : IObservable<CommandOrEvent>
{
}

public delegate CommandOrEvent ReadMessage(CancellationToken cancellationToken = default);

public class CommandAndEventReciever : IKernelCommandAndEventReceiver2, IDisposable
{
    private readonly ReadMessage _readMessage;
    private readonly Subject<CommandOrEvent> _subject = new();
    private readonly IObservable<CommandOrEvent> _observable;
    private readonly CompositeDisposable _disposables = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public CommandAndEventReciever(ReadMessage readMessage)
    {
        _readMessage = readMessage;

        _disposables.Add(Disposable.Create(() => _cancellationTokenSource.Cancel()));

        _observable = Observable.Defer(
                                    () => Observable.Create<CommandOrEvent>(observer =>
                                    {
                                        var subscription =
                                            _subject
                                                .ObserveOn(new EventLoopScheduler())
                                                .Subscribe(observer);

                                        var thread = new Thread(ReaderLoop);
                                        thread.Name = $"{nameof(CommandAndEventReciever)} loop ({GetHashCode()})";

                                        thread.Start();

                                        return subscription;
                                    }))
                                .Publish()
                                .RefCount();
    }

    private void ReaderLoop()
    {
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var message = _readMessage(_cancellationTokenSource.Token);

                _subject.OnNext(message);
            }
        }
        catch
        {
        }
    }

    public IDisposable Subscribe(IObserver<CommandOrEvent> observer)
    {
        return _observable.Subscribe(observer);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}