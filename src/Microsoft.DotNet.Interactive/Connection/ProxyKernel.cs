// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Connection;

public sealed class ProxyKernel : Kernel
{
    private readonly IKernelCommandAndEventSender _sender;
    private readonly IKernelCommandAndEventReceiver _receiver;
    private ExecutionContext _executionContext;

    private readonly Dictionary<string, (KernelCommand command, ExecutionContext executionContext, TaskCompletionSource<KernelEvent> completionSource, KernelInvocationContext
        invocationContext)> _inflight = new();

    private IKernelValueDeclarer _valueDeclarer;
    private readonly Uri _remoteUri;

    public ProxyKernel(
        string name,
        IKernelCommandAndEventSender sender,
        IKernelCommandAndEventReceiver receiver,
        Uri remoteUri = null) : base(name)
    {
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));

        if (remoteUri is not null)
        {
            _remoteUri = remoteUri;
        }
        else if (sender.RemoteHostUri is { } remoteHostUri)
        {
            _remoteUri = new(remoteHostUri, name);
        }
        else
        {
            throw new ArgumentNullException(nameof(remoteUri));
        }

        KernelInfo.RemoteUri = _remoteUri;

        var subscription = _receiver.Subscribe(coe =>
        {
            if (coe.Event is { } e)
            {
                DelegatePublication(e);
            }
        });

        RegisterForDisposal(subscription);
    }

    private Task HandleByForwardingToRemoteAsync(KernelCommand command, KernelInvocationContext context)
    {
        switch (command)
        {
            case AnonymousKernelCommand:
                return base.HandleAsync(command, context);
            case DirectiveCommand:
                return base.HandleAsync(command, context);
        }

        if (command.OriginUri is null)
        {
            if (context.HandlingKernel == this)
            {
                command.OriginUri = KernelInfo.Uri;
            }
        }

        _executionContext = ExecutionContext.Capture();
        var token = command.GetOrCreateToken();

        command.OriginUri ??= KernelInfo.Uri;

        var targetKernelName = command.TargetKernelName;
        command.TargetKernelName = null;
        var completionSource = new TaskCompletionSource<KernelEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        _inflight[token] = (command, _executionContext, completionSource, context);

        ExecutionContext.SuppressFlow();
        var _ = _sender.SendAsync(command, context.CancellationToken);
        return completionSource.Task.ContinueWith(te =>
        {
            command.TargetKernelName = targetKernelName;
            if (te.Result is CommandFailed cf)
            {
                context.Fail(command, cf.Exception, cf.Message);
            }
        });
    }

    internal override Task HandleAsync(
        KernelCommand command, 
        KernelInvocationContext context) =>
        HandleByForwardingToRemoteAsync(command, context);

    public override Task HandleAsync(RequestKernelInfo command, KernelInvocationContext context) =>
        // override the default handler on Kernel and forward the command instead
        HandleByForwardingToRemoteAsync(command, context);

    private void DelegatePublication(KernelEvent kernelEvent)
    {
        var token = kernelEvent.Command.GetOrCreateToken();

        var hasPending = _inflight.TryGetValue(token, out var pending);

        if (hasPending && HasSameOrigin(kernelEvent, KernelInfo))
        {
            switch (kernelEvent)
            {
                case CommandFailed cf when pending.command.IsEquivalentTo(kernelEvent.Command):
                    _inflight.Remove(token);
                    pending.completionSource.TrySetResult(cf);
                    break;

                case CommandSucceeded cs when pending.command.IsEquivalentTo(kernelEvent.Command):
                    _inflight.Remove(token);
                    pending.completionSource.TrySetResult(cs);
                    break;

                default:
                    if (pending.executionContext is { } ec)
                    {
                        ExecutionContext.Run(ec, _ => pending.invocationContext.Publish(kernelEvent), null);
                    }
                    else
                    {
                        pending.invocationContext.Publish(kernelEvent);
                    }
                    break;
            }
        }
    }

    private bool HasSameOrigin(KernelEvent kernelEvent, KernelInfo kernelInfo)
    {
        var commandOriginUri = kernelEvent.Command.OriginUri;

        if (commandOriginUri is null)
        {
            commandOriginUri = KernelInfo.Uri;
        }

        if (kernelInfo is not null && 
            commandOriginUri is not null)
        {
            return commandOriginUri.Equals(kernelInfo.Uri);
        }

        return commandOriginUri is null;
    }

    internal IKernelValueDeclarer ValueDeclarer
    {
        set => _valueDeclarer = value;
    }

    public override IKernelValueDeclarer GetValueDeclarer() => _valueDeclarer ?? KernelValueDeclarer.Default;
}