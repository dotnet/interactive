// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

internal class StdInChannel
{
    private readonly MessageSender _sender;
    private readonly MessageReceiver _receiver;

    public StdInChannel(MessageSender sender, MessageReceiver receiver)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _receiver = receiver ?? throw new ArgumentNullException(nameof(sender));
    }

    public string RequestInput(InputRequest message, Message request)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        Send(
            Message.Create(
                message,
                request.Header,
                request.Identifiers,
                request.MetaData,
                request.Signature,
                MessageChannelValues.stdin));

        var msgReceived = _receiver.Receive();
        var msgType = msgReceived.Header.MessageType;

        if (msgType == JupyterMessageContentTypes.InputReply)
        {
            if (msgReceived.Content is InputReply inputReply)
            {
                return inputReply.Value;
            }

            throw new InvalidOperationException(
                $"The content of a '{msgType}' message should be a {nameof(InputReply)} object.");
        }

        throw new ArgumentOutOfRangeException($"Expecting an 'input_reply' message, but received '{msgType}'.");
    }

    public void Send(Message message)
    {
        _sender.Send(message);
    }
}
