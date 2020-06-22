// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ShutdownRequestHandler : RequestHandlerBase<ShutdownRequest>
    {
        public ShutdownRequestHandler(IKernel kernel, IScheduler scheduler) : base(kernel, scheduler)
        {

        }


        protected override void OnKernelEventReceived(KernelEvent @event, JupyterRequestContext context)
        {
           
        }

        public Task Handle(JupyterRequestContext context)
        {
            var shutdownReplyPayload = new ShutdownReply();
            context.JupyterMessageSender.Send(shutdownReplyPayload);
            Environment.Exit(0);
            return Task.CompletedTask;
        }
    }
}