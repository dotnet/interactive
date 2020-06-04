// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;


namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterRequestContextHandler : ICommandHandler<JupyterRequestContext>
    {
        private readonly ExecuteRequestHandler _executeHandler;
        private readonly CompleteRequestHandler _completeHandler;
        private readonly InterruptRequestHandler _interruptHandler;
        private readonly IsCompleteRequestHandler _isCompleteHandler;
        private readonly ShutdownRequestHandler _shutDownHandler;

        public JupyterRequestContextHandler(IKernel kernel)
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
            _shutDownHandler = new ShutdownRequestHandler(kernel, scheduler);
        }

        public async Task<ICommandDeliveryResult> Handle(
            ICommandDelivery<JupyterRequestContext> delivery)
        {
            switch (delivery.Command.JupyterRequestMessageEnvelope.Content)
            {
                case ExecuteRequest _:
                    await _executeHandler.Handle(delivery.Command);
                    break;
                case CompleteRequest _:
                    await _completeHandler.Handle(delivery.Command);
                    break;
                case InterruptRequest _:
                    await _interruptHandler.Handle(delivery.Command);
                    break;
                case IsCompleteRequest _:
                    await _isCompleteHandler.Handle(delivery.Command);
                    break;
                case ShutdownRequest _:
                    await _shutDownHandler.Handle(delivery.Command);
                    break;
                default:
                    break;
            }

            delivery.Command.Complete();

            return delivery.Complete();
        }
    }
}