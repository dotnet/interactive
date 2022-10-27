// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class SubmitCodeHandler : CommandToJupyterMessageHandlerBase<SubmitCode>
{
    public SubmitCodeHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
    {
    }

    public override async Task HandleCommandAsync(SubmitCode command, ICommandExecutionContext context, CancellationToken token)
    {
        var executeRequest = Messaging.Message.Create(new ExecuteRequest(command.Code.NormalizeLineEndings()));
        var executeReply = Receiver.Messages.FilterByParent(executeRequest)
                                .SelectContent()
                                .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                .TakeUntil(m => m.MessageType == JupyterMessageContentTypes.Error
                                || (m.MessageType == JupyterMessageContentTypes.Status && (m as Status)?.ExecutionState == StatusValues.Idle));
        // run until kernel idle

        await Sender.SendAsync(executeRequest);
        await executeReply.ToTask(token);
    }

    private void HandleReplyMessage(Protocol.Message message, SubmitCode command, ICommandExecutionContext context)
    {
        switch (message)
        {
            case (Status status):
                if (status.ExecutionState == StatusValues.Idle)
                {
                    // Since Error will trigger CommandFailed event before Status.Idle is reported, assume that 
                    // status idle means command is successful.
                    context.Publish(new CommandSucceeded(command));
                }
                break;

            case (DisplayData displayData):
                context.Publish(new DisplayedValueProduced(displayData.Data,
                                                        command,
                                                        GetFormattedValues(displayData.Data),
                                                        GetDisplayIdFromTransientData(displayData.Transient)
                                                        ));
                break;

            case (UpdateDisplayData updateDisplayData):
                context.Publish(new DisplayedValueUpdated(updateDisplayData.Data,
                                                          GetDisplayIdFromTransientData(updateDisplayData.Transient),
                                                          command,
                                                          GetFormattedValues(updateDisplayData.Data)));
                break;
            case (ExecuteResult result):
                context.Publish(new ReturnValueProduced(result.Data,
                                                        command,
                                                        GetFormattedValues(result.Data)));
                break;
            case (Stream streamResult):
                if (streamResult.Name == Stream.StandardOutput)
                {
                    context.Publish(
                        new StandardOutputValueProduced(
                            command,
                            new[] { new FormattedValue(PlainTextFormatter.MimeType, streamResult.Text) }));
                }

                if (streamResult.Name == Stream.StandardError)
                {
                    context.Publish(
                        new StandardErrorValueProduced(
                            command,
                            new[] { new FormattedValue(PlainTextFormatter.MimeType, streamResult.Text) }));
                }
                break;
            case (Error error):
                // TODO: how to translate traceback to exception;
                context.Publish(new CommandFailed(null, command, error.EValue));
                break;
            default:
                break;
        }
    }

    private string GetDisplayIdFromTransientData(IReadOnlyDictionary<string, object> transientData)
    {
        string key = "display_id";
        return transientData.ContainsKey(key) ? transientData[key]?.ToString() : Guid.NewGuid().ToString();
    }

    private IReadOnlyCollection<FormattedValue> GetFormattedValues(IReadOnlyDictionary<string, object> data)
    {
        var formattedValues = data.Select(d => new FormattedValue(d.Key, d.Value.ToString())).ToArray();
        return formattedValues;
    }
}
