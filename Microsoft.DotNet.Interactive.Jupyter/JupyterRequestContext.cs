// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterRequestContext
    {
        private readonly TaskCompletionSource<Unit> _done = new TaskCompletionSource<Unit>();

        internal JupyterRequestContext(ReplyChannel serverChannel, PubSubChannel ioPubChannel, StdInChannel stdInChannel, ZeroMQMessage request, string kernelIdentity)
            : this(new JupyterMessageSender(ioPubChannel, serverChannel, stdInChannel, kernelIdentity, request), request)
        {
        }

        public JupyterRequestContext(IJupyterMessageSender jupyterMessageSender, ZeroMQMessage request)
        {
            Token = Guid.NewGuid().ToString("N");
            JupyterMessageSender = jupyterMessageSender ?? throw new ArgumentNullException(nameof(jupyterMessageSender));
            JupyterRequestMessageEnvelope = request ?? throw new ArgumentNullException(nameof(request));
        }

        public string Token { get; }

        public IJupyterMessageSender JupyterMessageSender { get; }

        public ZeroMQMessage JupyterRequestMessageEnvelope { get; }

        public T GetRequestContent<T>() where T : RequestMessage
        {
            return JupyterRequestMessageEnvelope?.Content as T;
        }

        public void Complete() => _done.SetResult(Unit.Default);

        public Task Done() => _done.Task;
    }
}