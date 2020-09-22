// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;


namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterRequestContextHandler 
    {
        private readonly ExecuteRequestHandler _executeHandler;
        private readonly CompleteRequestHandler _completeHandler;
        private readonly InterruptRequestHandler _interruptHandler;
        private readonly IsCompleteRequestHandler _isCompleteHandler;
        private readonly ShutdownRequestHandler _shutdownHandler;

        public JupyterRequestContextHandler(Kernel kernel)
        {
            var scheduler = new EventLoopScheduler(t =>
            {
                var thread = new Thread(t) {IsBackground = true, Name = "MessagePump"};
                return thread;
            });
            
            _executeHandler = new ExecuteRequestHandler(kernel, scheduler);
            _completeHandler = new CompleteRequestHandler(kernel, scheduler);
            _interruptHandler = new InterruptRequestHandler(kernel, scheduler);
            _isCompleteHandler = new IsCompleteRequestHandler(kernel, scheduler);
            _shutdownHandler = new ShutdownRequestHandler(kernel, scheduler);
        }

        public async Task Handle(JupyterRequestContext context)
        {
            switch (context.JupyterRequestMessageEnvelope.Content)
            {
                case ExecuteRequest _:
                    await _executeHandler.Handle(context);
                    break;
                case CompleteRequest _:
                    await _completeHandler.Handle(context);
                    break;
                case InterruptRequest _:
                    await _interruptHandler.Handle(context);
                    break;
                case IsCompleteRequest _:
                    await _isCompleteHandler.Handle(context);
                    break;
                case ShutdownRequest _:
                    await _shutdownHandler.Handle(context);
                    break;
            }

            context.Complete();
        }
    }
}