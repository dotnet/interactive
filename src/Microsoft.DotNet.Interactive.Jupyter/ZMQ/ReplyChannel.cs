// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    internal class ReplyChannel
    {
        private readonly MessageSender _sender;

        public ReplyChannel(MessageSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }
        public void Reply(ReplyMessage message, Message request)
        {
            var reply = Message.CreateReply(message, request);
            _sender.Send(reply);
        }
    }
}