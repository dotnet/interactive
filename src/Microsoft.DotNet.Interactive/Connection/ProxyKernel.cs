// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection;

public sealed class ProxyKernel : Kernel
{
    private readonly IKernelCommandAndEventSender _sender;
    private readonly IKernelCommandAndEventReceiver _receiver;
    private ExecutionContext _executionContext;
    private string _suppressCompletionsForCommandId;

    private readonly Dictionary<string, (KernelCommand command, ExecutionContext executionContext, TaskCompletionSource<KernelEvent> completionSource, KernelInvocationContext
        invocationContext)> _inflight = new();

    public ProxyKernel(
        string name,
        IKernelCommandAndEventSender sender,
        IKernelCommandAndEventReceiver receiver,
        Uri remoteUri = null) : base(name)
    {
        KernelInfo.IsProxy = true;
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));

        if (remoteUri is not null)
        {
            KernelInfo.RemoteUri = remoteUri;
        }
        else if (sender.RemoteHostUri is { } remoteHostUri)
        {
            KernelInfo.RemoteUri = new(remoteHostUri, name);
        }
        else
        {
            throw new ArgumentNullException(nameof(remoteUri));
        }

        var subscription = _receiver.Subscribe(coe =>
        {
            if (coe.Event is { } e)
            {
                if (e is KernelInfoProduced { Command: NoCommand } kip && kip.KernelInfo.Uri == KernelInfo.RemoteUri)
                {
                    UpdateKernelInfoFromEvent(kip);
                    PublishEvent(new KernelInfoProduced(KernelInfo, e.Command));
                }
                else
                {
                    DelegatePublication(e);
                }
            }
        });

        RegisterForDisposal(subscription);
    }

    private void UpdateKernelInfoFromEvent(KernelInfoProduced kernelInfoProduced)
    {
        var kernelInfo = kernelInfoProduced.KernelInfo;
        UpdateKernelInfo(kernelInfo);
    }

    private Task HandleByForwardingToRemoteAsync(KernelCommand command, KernelInvocationContext context)
    {
        if (command.OriginUri is null)
        {
            if (context.HandlingKernel == this)
            {
                command.OriginUri = KernelInfo.Uri;
            }
        }

        _executionContext = ExecutionContext.Capture();
        var token = command.GetOrCreateToken();
        command.GetOrCreateId();

        command.OriginUri ??= KernelInfo.Uri;

        if (command.DestinationUri is null)
        {
            command.DestinationUri = KernelInfo.RemoteUri;
        }

        if (command is RequestKernelInfo requestKernelInfo)
        {
            if (requestKernelInfo.RoutingSlip.Contains(KernelInfo.RemoteUri, true))
            {
                return Task.CompletedTask;
            }
        }

        var targetKernelName = command.TargetKernelName;
        if (command.TargetKernelName == Name)
        {
            command.TargetKernelName = null;
        }

        var completionSource = new TaskCompletionSource<KernelEvent>();

        _inflight[token] = (command, _executionContext, completionSource, context);

        ExecutionContext.SuppressFlow();

        var t = _sender.SendAsync(command, context.CancellationToken);
        t.ContinueWith(task =>
        {
            if (!task.GetIsCompletedSuccessfully())
            {
                if (task.Exception is { } ex)
                {
                    completionSource.TrySetException(ex);
                }
            }
        });

        return completionSource.Task.ContinueWith(te =>
        {
            command.TargetKernelName = targetKernelName;

            if (te.Result is CommandFailed cf)
            {
                context.Fail(command, cf.Exception, cf.Message);
            }
        });
    }

    private bool CanHandleLocally(KernelCommand command)
    {
        if (!CanHandle(command))
        {
            return false;
        }

        if (HasDynamicHandlerFor(command))
        {
            return true;
        }

        return false;
    }

    protected override Func<TCommand, KernelInvocationContext, Task> CreateDefaultHandlerForCommandType<TCommand>()
    {
        return HandleByForwardingToRemoteAsync;
    }

    internal override Task HandleAsync(
        KernelCommand command,
        KernelInvocationContext context)
    {
        switch (command)
        {
            case AnonymousKernelCommand:
                return base.HandleAsync(command, context);
            case DirectiveCommand:
                return base.HandleAsync(command, context);
        }

        if (CanHandleLocally(command))
        {
            return base.HandleAsync(command, context);
        }

        return HandleByForwardingToRemoteAsync(command, context);
    }

    private protected override Task HandleRequestKernelInfoAsync(RequestKernelInfo command, KernelInvocationContext context) =>
        // override the default handler on Kernel and forward the command instead
        HandleByForwardingToRemoteAsync(command, context);

    private void DelegatePublication(KernelEvent kernelEvent)
    {
        var token = kernelEvent.Command.GetOrCreateToken();

        var hasPending = _inflight.TryGetValue(token, out var pending);

        if (hasPending && HasSameOrigin(kernelEvent, KernelInfo))
        {
            if (kernelEvent.Command.IsEquivalentTo(pending.command))
            {
                pending.command.RoutingSlip.ContinueWith(kernelEvent.Command.RoutingSlip);
            }
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
                case CommandFailed _ when kernelEvent.Command.GetOrCreateId() == _suppressCompletionsForCommandId:
                case CommandSucceeded _ when kernelEvent.Command.GetOrCreateId() == _suppressCompletionsForCommandId:
                    _suppressCompletionsForCommandId = null;
                    break;
                case KernelInfoProduced kip when kip.KernelInfo.Uri == KernelInfo.RemoteUri:
                    {
                        UpdateKernelInfoFromEvent(kip);
                        var newEvent = new KernelInfoProduced(KernelInfo, kernelEvent.Command);

                        newEvent.RoutingSlip.ContinueWith(kip.RoutingSlip);

                        if (pending.executionContext is { } ec)
                        {
                            ExecutionContext.Run(ec, _ =>
                            {
                                pending.invocationContext.Publish(newEvent);
                                pending.invocationContext.Publish(kip);
                            }, null);
                        }
                        else
                        {
                            pending.invocationContext.Publish(newEvent);
                            pending.invocationContext.Publish(kip);
                        }
                    }
                    break;
                default:
                    {
                        if (pending.executionContext is { } ec)
                        {
                            ExecutionContext.Run(ec, _ => pending.invocationContext.Publish(kernelEvent), null);
                        }
                        else
                        {
                            pending.invocationContext.Publish(kernelEvent);
                        }
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

    public void UpdateKernelInfo(KernelInfo kernelInfo)
    {
        KernelInfo.IsComposite = kernelInfo.IsComposite;
        KernelInfo.LanguageName = kernelInfo.LanguageName;
        KernelInfo.LanguageVersion = kernelInfo.LanguageVersion;
        KernelInfo.UpdateSupportedKernelCommandsFrom(kernelInfo);
        ((HashSet<KernelDirectiveInfo>)KernelInfo.SupportedDirectives).UnionWith(kernelInfo.SupportedDirectives);
    }
}