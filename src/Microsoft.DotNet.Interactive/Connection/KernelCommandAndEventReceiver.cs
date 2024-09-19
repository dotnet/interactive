// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using static Pocket.Logger;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive.Connection;

public class KernelCommandAndEventReceiver : IKernelCommandAndEventReceiver, IDisposable
{
    private readonly ReadCommandOrEventAsync _readCommandOrEvent;
    private readonly Subject<CommandOrEvent> _subject = new();
    private readonly IObservable<CommandOrEvent> _observable;
    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _cancellationTokenSource;

    public KernelCommandAndEventReceiver(ReadCommandOrEventAsync readCommandOrEvent)
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
                                        thread.IsBackground = true;
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

    public KernelCommandAndEventReceiver(ReadCommandOrEvent readCommandOrEvent) :
        this(async token =>
        {
            if (!token.IsCancellationRequested)
            {
                var commandOrEvent = readCommandOrEvent();

                return await Task.FromResult(commandOrEvent);
            }
            else
            {
                return default;
            }
        })
    {
    }

    private KernelCommandAndEventReceiver(IObservable<string> messages) =>
        _observable = messages
            .Select(s =>
            {
                try
                {
                    return Serializer.DeserializeCommandOrEvent(s);
                }
                catch (Exception exception)
                {
                    Log.Error(exception);

                    return new CommandOrEvent(new ErrorProduced(exception.Message, KernelCommand.None), isParseError: true);
                }
            });

    private void ReaderLoop()
    {
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var message = _readCommandOrEvent(_cancellationTokenSource.Token).GetAwaiter().GetResult();

                if (message is not null)
                {
                    _subject.OnNext(message);
                }
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception);
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
            catch (Exception exception)
            {
                Log.Error(exception);
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
        new(async token =>
        {
            try
            {
#if NETSTANDARD2_0
                var json = await reader.ReadLineAsync();
#else
                var timedCts = new CancellationTokenSource(1000);
                var json = await reader.ReadLineAsync(timedCts.Token);
#endif

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);
                    return commandOrEvent;
                }

                return null;
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
        new(async token =>
        {
            if (stream.CanRead)
            {
                var json = await stream.ReadMessageAsync(token);

                var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);

                return commandOrEvent;
            }
            else
            {
                return null;
            }
        });
}