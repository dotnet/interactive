// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class RecordingJupyterMessageSender : IJupyterMessageSender
    {
        private readonly List<Message> _messages;
        private readonly List<PubSubMessage> _pubSubMessages;
        private readonly List<ReplyMessage> _replyMessages;
        private readonly List<InputRequest> _requestMessages;
        public IReadOnlyList<Message> Messages => _messages;

        public IEnumerable<ReplyMessage> ReplyMessages => _replyMessages;
        public IEnumerable<PubSubMessage> PubSubMessages => _pubSubMessages;
        public IEnumerable<InputRequest> RequestMessages => _requestMessages;

        public RecordingJupyterMessageSender()
        {
            _messages = new List<Message>();
            _pubSubMessages = new List<PubSubMessage>();
            _replyMessages = new List<ReplyMessage>();
            _requestMessages = new List<InputRequest>();
        }

        public void Send(PubSubMessage message)
        {
           _messages.Add(message);
           _pubSubMessages.Add(message);
        }

        public void Send(ReplyMessage message)
        {
            _messages.Add(message);
            _replyMessages.Add(message);
        }

        public string Send(InputRequest message)
        {
            _messages.Add(message);
            _requestMessages.Add(message);

            return $"input-value";
        }
    }
}
