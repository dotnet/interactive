// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal partial class JupyterKernel
    : IKernelCommandHandler<SubmitCode>,
      IKernelCommandHandler<RequestCompletions>,
      IKernelCommandHandler<RequestHoverText>,
      IKernelCommandHandler<RequestSignatureHelp>
{
    public async Task HandleAsync(RequestHoverText command, KernelInvocationContext context)
    {
        var request = new InspectRequest(code: command.Code,
                                         cursorPos: SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition),
                                         detailLevel: 0);

        var results = await RunOnKernelAsync<InspectReply>(request, context.CancellationToken);

        if (results.Status != StatusValues.Ok)
        {
            context.Fail(command);
            return;
        }

        if (results.Found)
        {
            var content = results.Data.Select(d => new FormattedValue(d.Key,
                                                                      d.Value
                                                                        .ToString()
                                                                        .StripUnsupportedTextFormats())
                                                                      ).ToArray();

            // we don't get any line position back from kernel
            context.Publish(new HoverTextProduced(command, content, new LinePositionSpan(command.LinePosition, command.LinePosition)));
        }
    }

    public async Task HandleAsync(RequestSignatureHelp command, KernelInvocationContext context)
    {
        var request = new InspectRequest(code: command.Code,
                                         cursorPos: SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition),
                                         detailLevel: 0);

        var results = await RunOnKernelAsync<InspectReply>(request, context.CancellationToken);

        if (results.Status != StatusValues.Ok)
        {
            context.Fail(command);
            return;
        }

        if (results.Found)
        {
            // the kernels return everything as a docstring. 
            var signatureHelpItems = results.Data.Select(d => new SignatureInformation(string.Empty,
                                                                                       new FormattedValue(d.Key,
                                                                                                          d.Value
                                                                                                            .ToString()
                                                                                                            .StripUnsupportedTextFormats()),
                                                                                       new List<ParameterInformation>())).ToArray();

            context.Publish(new SignatureHelpProduced(command, signatureHelpItems, 0, 0));
        }
    }

    public async Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
    {
        var request = new CompleteRequest(
                            command.Code,
                            SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition));

        var results = await RunOnKernelAsync<CompleteReply>(request, context.CancellationToken);

        if (results.Status != StatusValues.Ok)
        {
            context.Fail(command);
            return;
        }

        bool metadataAvailable = results.MetaData.TryGetValue(CompletionResultMetadata.Experimental, out IReadOnlyList<CompletionResultMetadata> resultsMetadata);

        string GetCompletionKind(string type)
        {
            var kind = type switch
            {
                "keyword" => "Keyword",
                "function" => "Method",
                "class" => "Class",
                "instance" => "Field",
                "module" => "Module",
                "type" => "Class",
                "property" => "Property",
                "field" => "Field",
                "builtin" => "Method",
                "builtinfunction" => "Method",
                "builtinmodule" => "Module",
                "builtintype" => "Class",
                "value" => "Variable",
                "constant" => "Constant",
                "variable" => "Variable",
                _ => string.Empty
            };
            return kind;
        };

        var completionItems = metadataAvailable ?
               resultsMetadata.Select(m =>
                             new CompletionItem(
                                 displayText: m.DisplayText ?? m.Text,
                                 kind: GetCompletionKind(m.Type ?? string.Empty),
                                 insertText: m.Text,
                                 filterText: m.Text,
                                 sortText: m.Text))
                 : results.Matches.Select(match =>
                             new CompletionItem(
                                    displayText: match,
                                    kind: string.Empty,
                                    insertText: match,
                                    filterText: match,
                                    sortText: match));


        var completion = new CompletionsProduced(
            completionItems, command,
            SourceUtilities.GetLinePositionSpanFromStartAndEndIndices(command.Code, results.CursorStart, results.CursorEnd));

        context.Publish(completion);
    }
    
    public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        CancelCommandOnKernelIfRequested(context);

        ExecuteReply results = null;
        bool messagesProcessed = false;

        var executeRequest = Messaging.Message.Create(
                                    new ExecuteRequest(
                                        command.Code.NormalizeLineEndings(),
                                        allowStdin: false, // TODO: ZMQ stdin channel is hanging. Disable until a consistent experience can be turned on. 
                                        stopOnError: true));
        var processMessages = Receiver.Messages
                                .ResponseOf(executeRequest)
                                .Content()
                                .Do(async m =>
                                {
                                    if (m is ExecuteReply reply)
                                    {
                                        results = reply;
                                    }
                                    else if (m is Status status && status.ExecutionState == StatusValues.Idle)
                                    {
                                        messagesProcessed = true;
                                    }
                                    else
                                    {
                                        await HandleExecuteReplyMessageAsync(m, command, context);
                                    }
                                })
                                .TakeUntil(m => m.MessageType == JupyterMessageContentTypes.Error || (messagesProcessed && results is not null));
        // run until kernel idle or until execution is done

        await Sender.SendAsync(executeRequest);
        await processMessages.ToTask(context.CancellationToken);

        if (results is not null && results.Status != StatusValues.Ok)
        {
            context.Fail(command);
        }
    }

    private async Task HandleExecuteReplyMessageAsync(Protocol.Message message, SubmitCode command, KernelInvocationContext context)
    {
        switch (message)
        {
            case (DisplayData displayData):
                context.Publish(new DisplayedValueProduced(displayData.Data,
                                                        command,
                                                        GetFormattedValuesFromMimeBundle(displayData.Data),
                                                        GetDisplayIdFromTransientData(displayData.Transient)
                                                        ));
                break;

            case (UpdateDisplayData updateDisplayData):
                context.Publish(new DisplayedValueUpdated(updateDisplayData.Data,
                                                          GetDisplayIdFromTransientData(updateDisplayData.Transient),
                                                          command,
                                                          GetFormattedValuesFromMimeBundle(updateDisplayData.Data)));
                break;
            case (ExecuteResult result):
                context.Publish(new ReturnValueProduced(result.Data,
                                                        command,
                                                        GetFormattedValuesFromMimeBundle(result.Data)));
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

                StringBuilder builder = new StringBuilder();
                foreach (var item in error.Traceback)
                {
                    builder.AppendLine(item);
                }

                // TODO: Change to use ErrorProduced once the extension bug is fixed to ensure error message is displayed correctly. 
                // For now, use StandardErrorValueProduced instead to provide the tracebacks.
                context.Publish(
                        new StandardErrorValueProduced(
                            command,
                            new[] { new FormattedValue(PlainTextFormatter.MimeType, builder.ToString()) }));

                context.Fail(command, null, error.EName);
                break;
            case (InputRequest inputRequest):

                string input = await GetInputAsync(inputRequest, context);
                var reply = new InputReply(input);
                await Sender.SendAsync(Messaging.Message.Create(reply, channel: MessageChannelValues.stdin));

                break;
            default:
                break;
        }
    }

    private string GetDisplayIdFromTransientData(IReadOnlyDictionary<string, object> transientData)
    {
        const string key = "display_id";
        return transientData.ContainsKey(key) ? transientData[key]?.ToString() : Guid.NewGuid().ToString();
    }

    private IReadOnlyCollection<FormattedValue> GetFormattedValuesFromMimeBundle(IReadOnlyDictionary<string, object> data)
    {
        var formattedValues = data.Select(d => new FormattedValue(d.Key, d.Value.ToString())).ToArray();
        return formattedValues;
    }

    private async Task<string> GetInputAsync(InputRequest inputRequest, KernelInvocationContext context)
    {
        var command = new RequestInput(
                inputRequest.Prompt,
                inputRequest.Password ? "password" : default);

        var results = await Root.SendAsync(command, CancellationToken.None);

        var failedEvent = results.Events.OfType<CommandFailed>().FirstOrDefault();
        if (failedEvent is { })
        {
            context.Fail(context.Command, null, failedEvent.Message);
        }

        var inputProduced = results.Events
                                         .OfType<InputProduced>()
                                         .FirstOrDefault();

        return inputProduced?.Value;
    }

    private async Task InterruptKernelExecutionAsync()
    {
        await RunOnKernelAsync<InterruptReply>(new InterruptRequest(),
                                               CancellationToken.None,
                                               channel: MessageChannelValues.control);
    }

    private void CancelCommandOnKernelIfRequested(KernelInvocationContext context)
    {
        context.CancellationToken.Register(async () =>
        {
            try
            {
                await InterruptKernelExecutionAsync();
            }
            catch (ObjectDisposedException) { } // TODO: workaround as object dispose is unpredictable and cell cancellation is ending up hitting disposed objects. 
        });
    }
}
