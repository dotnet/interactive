// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal abstract class CommandToJupyterMessageHandlerBase<TCommand> : IKernelCommandToMessageHandler<TCommand> where TCommand: KernelCommand
{
    private readonly IMessageSender _sender;
    private readonly IMessageReceiver _receiver;

    public CommandToJupyterMessageHandlerBase(IMessageSender sender, IMessageReceiver reciever)
    {
        _receiver = reciever;
        _sender = sender;
    }

    protected IMessageReceiver Receiver => _receiver;

    protected IMessageSender Sender => _sender;

    public abstract Task HandleCommandAsync(TCommand command, ICommandExecutionContext context, CancellationToken token);
}
