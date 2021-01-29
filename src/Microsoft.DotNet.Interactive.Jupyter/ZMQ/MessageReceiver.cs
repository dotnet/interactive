// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using NetMQ;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public class MessageReceiver
    {
        private readonly NetMQSocket _socket;

        public MessageReceiver(NetMQSocket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public Message Receive()
        {
            return _socket.GetMessage();
        }
    }
}
