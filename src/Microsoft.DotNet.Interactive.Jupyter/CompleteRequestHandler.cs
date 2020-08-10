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
        public CompleteRequestHandler(Kernel kernel,  IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var completeRequest = GetJupyterRequest(context);

            var position = SourceUtilities.GetPositionFromCursorOffset(completeRequest.Code, completeRequest.CursorPosition);
            var command = new RequestCompletions(completeRequest.Code, position);

            await SendAsync(context, command);
        }

        protected override void OnKernelEventReceived(
            KernelEvent @event, 
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case CompletionsProduced completionRequestCompleted:
                    OnCompletionRequestCompleted(
                        completionRequestCompleted, 
                        context.JupyterMessageSender);
                    break;
            }
        }

        private static void OnCompletionRequestCompleted(CompletionsProduced completionsProduced, IJupyterMessageSender jupyterMessageSender)
        {
            var startPosition = 0; 
            var endPosition = 0;

            if (completionsProduced.Command is RequestCompletions command)
            {
                if (completionsProduced.LinePositionSpan != null)
                {
                    startPosition = SourceUtilities.GetCursorOffsetFromPosition(command.Code, completionsProduced.LinePositionSpan.GetValueOrDefault().Start);
                    endPosition = SourceUtilities.GetCursorOffsetFromPosition(command.Code, completionsProduced.LinePositionSpan.GetValueOrDefault().End);
                }
                else
                {
                    var cursorOffset = SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition);
                    startPosition = SourceUtilities.ComputeReplacementStartPosition(command.Code, cursorOffset);
                    endPosition = cursorOffset;
                }
            }

            var reply = new CompleteReply(startPosition, endPosition, matches: completionsProduced.Completions.Select(e => e.InsertText ?? e.DisplayText).ToList());

            jupyterMessageSender.Send(reply);
        }
    }
}
