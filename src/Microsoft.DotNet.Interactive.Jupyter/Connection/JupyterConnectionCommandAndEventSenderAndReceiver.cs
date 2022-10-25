// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class JupyterConnectionCommandAndEventSenderAndReceiver : IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, ICommandExecutionContext, IDisposable
{
    private readonly Subject<CommandOrEvent> _commandOrEventsSubject;
    private readonly Uri _targetUri;
    private readonly CompositeDisposable _disposables;
    private readonly IMessageSender _sender;
    private readonly IMessageReceiver _receiver;

    private readonly ConcurrentDictionary<Type, Func<KernelCommand, ICommandExecutionContext, CancellationToken, Task>> _dynamicHandlers = new();

    public JupyterConnectionCommandAndEventSenderAndReceiver(IMessageSender messageSender, IMessageReceiver messageReceiver, Uri targetUri)
    {
        _commandOrEventsSubject = new Subject<CommandOrEvent>();
        _targetUri = targetUri;
        _receiver = messageReceiver;
        _sender = messageSender;

        var submitCodeHandler = new SubmitCodeHandler(messageSender, messageReceiver);
        var requestKernelInfoHandler = new RequestKernelInfoHandler(messageSender, messageReceiver);
        var completionsHandler = new RequestCompletionsHandler(messageSender, messageReceiver);
        var hoverTipHandler = new RequestHoverTextHandler(messageSender, messageReceiver);
        var sigHelpHandler = new RequestSignatureHelpHandler(messageSender, messageReceiver);

        RegisterCommandHandler<SubmitCode>(submitCodeHandler.HandleCommandAsync);
        RegisterCommandHandler<RequestKernelInfo>(requestKernelInfoHandler.HandleCommandAsync);
        RegisterCommandHandler<RequestCompletions>(completionsHandler.HandleCommandAsync);
        RegisterCommandHandler<RequestHoverText>(hoverTipHandler.HandleCommandAsync);
        RegisterCommandHandler<RequestSignatureHelp>(sigHelpHandler.HandleCommandAsync);

        _disposables = new CompositeDisposable
        {
            _commandOrEventsSubject
        };
    }

    public Uri RemoteHostUri => _targetUri;

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void RegisterCommandHandler<TCommand>(Func<TCommand, ICommandExecutionContext, CancellationToken, Task> handler)
        where TCommand : KernelCommand
    {
        _dynamicHandlers[typeof(TCommand)] = (command, context, token) => handler((TCommand)command, context, token);
    }


    private Func<KernelCommand, ICommandExecutionContext, CancellationToken, Task> TryGetDynamicHandler(KernelCommand command)
    {
        if (_dynamicHandlers.TryGetValue(command.GetType(), out var handler))
        {
            return handler;
        }
        return null;
    }

    public void Publish(KernelEvent kernelEvent)
    {
        var commandOrEvent = new CommandOrEvent(kernelEvent);
        _commandOrEventsSubject.OnNext(commandOrEvent);
    }

    public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
    {
        var handler = TryGetDynamicHandler(kernelCommand);
        if (handler != null)
        {
            await handler(kernelCommand, this, cancellationToken).ContinueWith(async (t) =>
            {
                if (t.IsCanceled)
                {
                    // trigger an explicit kernel interrupt as well to make sure the out-of-proc kernel 
                    // stops any running executions.
                    await InterruptKernelExecutionAsync();
                }
            }).Unwrap();
        }
    }

    public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
    {
        // TODO: could be used to translate events to jupyter message replies to the 
        // jupyter front end. 
        throw new NotImplementedException();
    }

    public IDisposable Subscribe(IObserver<CommandOrEvent> observer)
    {
        return _commandOrEventsSubject.Subscribe(observer);
    }

    private async Task InterruptKernelExecutionAsync()
    {
        var interruptRequest = Messaging.Message.Create(new InterruptRequest(), channel: "control");
        var interruptReply = _receiver.Messages.FilterByParent(interruptRequest)
                                .SelectContent()
                                .TakeUntilMessageType(JupyterMessageContentTypes.InterruptReply, JupyterMessageContentTypes.Error);

        await _sender.SendAsync(interruptRequest);
        await interruptReply.ToTask();
    }
}
