// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Connection;

public class KernelCommandAndEventReceiver : IKernelCommandAndEventReceiver, IDisposable
{
    private readonly ReadCommandOrEvent _readCommandOrEvent;
    private readonly Subject<CommandOrEvent> _subject = new();
    private readonly IObservable<CommandOrEvent> _observable;
    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _cancellationTokenSource;

    public KernelCommandAndEventReceiver(ReadCommandOrEvent readCommandOrEvent)
    {
        _readCommandOrEvent = readCommandOrEvent ?? throw new ArgumentNullException(nameof(readCommandOrEvent));

        _disposables.Add(Disposable.Create(TryCancelCancellationToken));

        _observable = Observable.Defer(
                                    () => Observable.Create<CommandOrEvent>(observer =>
                                    {
                                        _cancellationTokenSource = new();

                                        var subscription =
                                            _subject
                                                .ObserveOn(TaskPoolScheduler.Default)
                                                .Subscribe(observer);

                                        var thread = new Thread(ReaderLoop);
                                        thread.Name = $"{nameof(KernelCommandAndEventReceiver)} loop ({GetHashCode()})";

                                        thread.Start();

                                        return Disposable.Create(() =>
                                        {
                                            TryCancelCancellationToken();

                                            subscription.Dispose();
                                        });
                                    }))
                                .Publish()
                                .RefCount();
    }

    private KernelCommandAndEventReceiver(IObservable<string> messages) =>
        _observable = messages.ObserveOn(new EventLoopScheduler())
                              .Select(Serializer.DeserializeCommandOrEvent);

    private void ReaderLoop()
    {
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var message = _readCommandOrEvent(_cancellationTokenSource.Token);

                if (message is not null)
                {
                    _subject.OnNext(message);
                }
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

    private void TryCancelCancellationToken()
    {
        if (_cancellationTokenSource is { Token.CanBeCanceled: true } cts)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public static KernelCommandAndEventReceiver FromObservable(IObservable<string> messages) =>
        new(messages);

    public static KernelCommandAndEventReceiver FromTextReader(TextReader reader) =>
        new(_ =>
        {
            try
            {
                var json = reader.ReadLine();

                var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);

                return commandOrEvent;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        });

    public static KernelCommandAndEventReceiver FromNamedPipe(PipeStream stream) =>
        new(token =>
        {
            if (stream.CanRead)
            {
                var json = stream.ReadMessageAsync(token).GetAwaiter().GetResult();

                var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);

                return commandOrEvent;
            }
            else
            {
                return null;
            }
        });
}