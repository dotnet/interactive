// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public interface IKernelCommandAndEventReceiver
    {
        IAsyncEnumerable<CommandOrEvent> CommandsOrEventsAsync(CancellationToken cancellationToken);
    }

    public class KernelCommandAndEventObservableReceiver : KernelCommandAndEventReceiverBase, IDisposable
    {
        private readonly IObservable<string> _receiver;
        private readonly ConcurrentQueue<string> _queue;
        private readonly SemaphoreSlim _semaphore = new(0, 1);
        private readonly CompositeDisposable _disposables = new();

        public KernelCommandAndEventObservableReceiver(IObservable<string> receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _queue = new ConcurrentQueue<string>();
            _disposables.Add(
            _receiver.Subscribe(message =>
            {
                _queue.Enqueue(message);
                _semaphore.Release();
            }));
            _disposables.Add(_semaphore);
        }

        protected override async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_queue.TryDequeue(out var message))
            {
                return message;
            }
            await _semaphore.WaitAsync(cancellationToken);
            _queue.TryDequeue(out message);
            return message;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    public abstract class KernelCommandAndEventReceiverBase : IKernelCommandAndEventReceiver
    {
        protected abstract Task<string> ReadMessageAsync(CancellationToken cancellationToken);

        public async IAsyncEnumerable<CommandOrEvent> CommandsOrEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                KernelCommand kernelCommand = null;
                KernelEvent kernelEvent = null;

                var message = await ReadMessageAsync(cancellationToken);
                
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                var isParseError = false;
                try
                {
                    var jsonObject = JsonDocument.Parse(message).RootElement;
                    if (IsEventEnvelope(jsonObject))
                    {
                        var kernelEventEnvelope = KernelEventEnvelope.Deserialize(jsonObject);
                        kernelEvent = kernelEventEnvelope.Event;
                    }
                    else if (IsCommandEnvelope(jsonObject))
                    {
                        var kernelCommandEnvelope = KernelCommandEnvelope.Deserialize(jsonObject);
                        kernelCommand = kernelCommandEnvelope.Command;
                    }
                    else
                    {
                        kernelEvent = new DiagnosticLogEntryProduced(
                            $"Expected {nameof(KernelCommandEnvelope)} or {nameof(KernelEventEnvelope)} but received: \n{message}", KernelCommand.None);
                        isParseError = true;
                    }
                }
                catch (Exception ex)
                {
                    kernelEvent = new DiagnosticLogEntryProduced(
                        $"Error while parsing Envelope: {message} \n{ex.Message}", KernelCommand.None);
                    isParseError = true;
                }

                yield return kernelCommand is null ? new CommandOrEvent(kernelEvent, isParseError) : new CommandOrEvent(kernelCommand);
            }
        }

        private static bool IsEventEnvelope(JsonElement jsonObject)
        {
            if (jsonObject.TryGetProperty("eventType", out var eventType))
            {
                return !string.IsNullOrWhiteSpace(eventType.GetString());
            }

            return false;
        }

        private static bool IsCommandEnvelope(JsonElement jsonObject)
        {
            if (jsonObject.TryGetProperty("commandType", out var commandType))
            {
                return !string.IsNullOrWhiteSpace(commandType.GetString());
            }

            return false;
        }
    }
}