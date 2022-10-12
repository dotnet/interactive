// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class RequestCompletionsHandler : CommandToJupyterMessageHandlerBase<RequestCompletions>
{
    public RequestCompletionsHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
    {
    }

    public override async Task HandleCommandAsync(RequestCompletions command, ICommandExecutionContext context, CancellationToken token)
    {
        var request = Messaging.Message.Create(new CompleteRequest(
                                            command.Code,
                                            SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition)));

        var reply = Receiver.Messages.ChildOf(request)
                                .SelectContent()
                                .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                .TakeUntilMessageType(JupyterMessageContentTypes.CompleteReply, JupyterMessageContentTypes.Error);
        // run until we get a definitive pass or fail

        await Sender.SendAsync(request);
        await reply.ToTask(token);
    }

    private void HandleReplyMessage(Protocol.Message message, RequestCompletions command, ICommandExecutionContext context)
    {
        switch (message)
        {
            case (CompleteReply results):

                if (results.Status != StatusValues.Ok)
                {
                    // TODO: Add an error trace
                    context.Publish(new CommandFailed(null, command, "kernel returned failed"));
                    break;
                }

                bool metadataAvailable = results.MetaData.TryGetValue(CompletionResultMetadata.Entry, out IReadOnlyList<CompletionResultMetadata> resultsMetadata);
                if (!metadataAvailable)
                {
                    resultsMetadata = new List<CompletionResultMetadata>();
                }

                var completionItems = from match in results.Matches
                            join metadata in resultsMetadata on match equals metadata.Text into ci
                            from itemMetadata in ci.DefaultIfEmpty()
                            select new CompletionItem(
                                displayText: itemMetadata?.DisplayText ?? match,
                                kind: itemMetadata?.Type ?? string.Empty,
                                insertText: match,
                                filterText: match,
                                sortText: match);

                var completion = new CompletionsProduced(
                    completionItems, command,
                    SourceUtilities.GetLinePositionSpanFromStartAndEndIndices(command.Code, results.CursorStart, results.CursorEnd));

                context.Publish(completion);
                context.Publish(new CommandSucceeded(command));
                break;
            case (Error error):
                // TODO: how to translate traceback to exception;
                context.Publish(new CommandFailed(null, command, error.EValue));
                break;
            default:
                break;
        }
    }
}
