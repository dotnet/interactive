// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using NetMQ;
using Recipes;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public class MessageSender
    {
        private readonly IOutgoingSocket _socket;
        private readonly SignatureValidator _signatureValidator;

        public MessageSender(IOutgoingSocket socket, SignatureValidator signatureValidator)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        }

        public void Send(Message message)
        {
            var hmac = _signatureValidator.CreateSignature(message);

            if (message.Identifiers != null)
            {
                foreach (var ident in message.Identifiers)
                {
                    _socket.TrySendFrame(ident.ToArray(), true);
                }
            }

            Send(Constants.DELIMITER, _socket);
            Send(hmac, _socket);
            Send(message.Header.ToJson(), _socket);
            Send((message.ParentHeader?? new object()).ToJson(), _socket);
            Send((message.MetaData?? new object()).ToJson(), _socket);
            Send(message.Content.ToJson(), _socket, false);
        }

        private static void Send(string message, IOutgoingSocket socket, bool sendMore = true)
        {
            socket.SendFrame(message, sendMore);
        }
    }
}