// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    internal class JupyterMessageSender : IJupyterMessageSender
    {
        private readonly PubSubChannel _pubSubChannel;
        private readonly ReplyChannel _replyChannel;
        private readonly StdInChannel _stdInChannel;
        private readonly string _kernelIdentity;
        private readonly Message _request;

        public JupyterMessageSender(PubSubChannel pubSubChannel, ReplyChannel replyChannel, StdInChannel stdInChannel, string kernelIdentity, Message request)
        {
            if (string.IsNullOrWhiteSpace(kernelIdentity))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(kernelIdentity));
            }

            _pubSubChannel = pubSubChannel ?? throw new ArgumentNullException(nameof(pubSubChannel));
            _replyChannel = replyChannel ?? throw new ArgumentNullException(nameof(replyChannel));
            _stdInChannel = stdInChannel ?? throw new ArgumentNullException(nameof(stdInChannel));
            _kernelIdentity = kernelIdentity;
            _request = request;
        }

        public void Send(PubSubMessage message)
        {
            _pubSubChannel.Publish(message, _request, _kernelIdentity);}

        public void Send(ReplyMessage message)
        {
            _replyChannel.Reply(message, _request);
        }

        public string Send(InputRequest message)
        {
            return _stdInChannel.RequestInput(message, _request);
        }
    }
}