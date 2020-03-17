// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class CompleteRequestHandler : RequestHandlerBase<CompleteRequest>
    {
        public CompleteRequestHandler(IKernel kernel,  IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var completeRequest = GetJupyterRequest(context);

            var command = new RequestCompletion(completeRequest.Code, completeRequest.CursorPosition);

            await SendAsync(context, command);
        }

        protected override void OnKernelEventReceived(
            IKernelEvent @event, 
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case CompletionRequestCompleted completionRequestCompleted:
                    OnCompletionRequestCompleted(
                        completionRequestCompleted, 
                        context.JupyterMessageSender);
                    break;
            }
        }

        private static void OnCompletionRequestCompleted(CompletionRequestCompleted completionRequestCompleted,IJupyterMessageSender jupyterMessageSender)
        {
            var command = completionRequestCompleted.Command as RequestCompletion;

            var pos = SourceUtilities.ComputeReplacementStartPosition(command.Code, command.CursorPosition);
            var reply = new CompleteReply(pos, command.CursorPosition, matches: completionRequestCompleted.CompletionList.Select(e => e.InsertText ?? e.DisplayText).ToList());

            jupyterMessageSender.Send(reply);
        }
    }
}