// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Recipes;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class ExecuteRequestHandler : RequestHandlerBase<ExecuteRequest>
{
    private int _executionCount;

    public ExecuteRequestHandler(Kernel kernel, IScheduler scheduler = null)
        : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
    {
    }

    public async Task Handle(JupyterRequestContext context)
    {
        var executeRequest = GetJupyterRequest(context);
        var targetKernelName = context.GetKernelName();

        _executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);

        var executeInputPayload = new ExecuteInput(executeRequest.Code, _executionCount);
        context.JupyterMessageSender.Send(executeInputPayload);

        var command = new SubmitCode(executeRequest.Code, targetKernelName);

        await SendAsync(context, command);
    }

    protected override void OnKernelEventReceived(
        KernelEvent @event,
        JupyterRequestContext context)
    {
        switch (@event)
        {
            case DisplayEvent displayEvent:
                OnDisplayEvent(displayEvent, context.JupyterRequestMessageEnvelope, context.JupyterMessageSender);
                break;
            case CommandSucceeded _:
                OnCommandHandled(context.JupyterMessageSender);
                break;
            case CommandFailed commandFailed:
                OnCommandFailed(commandFailed, context.JupyterMessageSender);
                break;
            case DiagnosticsProduced diagnosticsProduced:
                OnDiagnosticsProduced(context, context.JupyterRequestMessageEnvelope, diagnosticsProduced);
                break;
        }
    }

    private static Dictionary<string, object> CreateTransient(string displayId = null)
    {
        var transient = new Dictionary<string, object> { { "display_id", displayId ?? Guid.NewGuid().ToString() } };
        return transient;
    }

    private void OnDiagnosticsProduced(JupyterRequestContext context,
        ZeroMQMessage request,
        DiagnosticsProduced diagnosticsProduced)
    {
        // Space out the diagnostics and send them to stderr
        if (diagnosticsProduced.FormattedDiagnostics.Count > 0)
        {
            var output =
                Environment.NewLine +
                string.Join(Environment.NewLine + Environment.NewLine, diagnosticsProduced.FormattedDiagnostics.Select(v => v.Value)) +
                Environment.NewLine +
                Environment.NewLine;
            var dataMessage = Protocol.Stream.StdErr(output);
            var isSilent = ((ExecuteRequest)request.Content).Silent;

            if (!isSilent)
            {
                // send on io
                context.JupyterMessageSender.Send(dataMessage);
            }
        }
    }

    private void OnCommandFailed(
        CommandFailed commandFailed,
        IJupyterMessageResponseSender jupyterMessageSender)
    {
        var traceBack = new List<string>();
        var emsg = commandFailed.Message;

        switch (commandFailed.Exception)
        {
            case CodeSubmissionCompilationErrorException _:
                // The diagnostics have already been reported
                emsg = "compilation error";
                break;

            case null:

                traceBack.Add(commandFailed.Message);
                break;

            default:
                var exception = commandFailed.Exception;

                traceBack.Add(exception.ToString());

                traceBack.AddRange(
                    exception.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

                break;
        }

        var errorContent = new Error(eValue: emsg, traceback: traceBack);

        // send on iopub
        jupyterMessageSender.Send(errorContent);

        //  reply Error
        var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: _executionCount);

        // send to server
        jupyterMessageSender.Send(executeReplyPayload);
    }

    private void OnDisplayEvent(DisplayEvent displayEvent,
        ZeroMQMessage request,
        IJupyterMessageResponseSender jupyterMessageSender)
    {
        if (displayEvent is ReturnValueProduced && displayEvent.Value is DisplayedValue)
        {
            return;
        }

        var transient = CreateTransient(displayEvent.ValueId);


        // Currently there is at most one formatted value with at most
        // and we return a dictionary for JSON formatting keyed by that mime type
        // 
        // In the case of DiagnosticsProduced however there are multiple entries, one
        // for each diagnsotic, all with the same type
        Dictionary<string, object> GetFormattedValuesByMimeType()
        {
            return
                displayEvent
                    .FormattedValues
                    .ToDictionary(k => k.MimeType, v => PreserveJson(v.MimeType, v.Value));
        }

        var value = displayEvent.Value;
        PubSubMessage dataMessage = null;

        switch (displayEvent)
        {
            case DisplayedValueProduced _:
                dataMessage = new DisplayData(
                    transient: transient,
                    data: GetFormattedValuesByMimeType());
                break;

            case DisplayedValueUpdated _:
                dataMessage = new UpdateDisplayData(
                    transient: transient,
                    data: GetFormattedValuesByMimeType());
                break;

            case ReturnValueProduced _:
                dataMessage = new ExecuteResult(
                    _executionCount,
                    transient: transient,
                    data: GetFormattedValuesByMimeType());
                break;

            case StandardOutputValueProduced _:
                dataMessage = Protocol.Stream.StdOut(GetPlainTextValueOrDefault(GetFormattedValuesByMimeType(), value?.ToString() ?? string.Empty));
                break;

            case StandardErrorValueProduced _:
            case ErrorProduced _:
                dataMessage = Protocol.Stream.StdErr(GetPlainTextValueOrDefault(GetFormattedValuesByMimeType(), value?.ToString() ?? string.Empty));
                break;

            default:
                throw new ArgumentException("Unsupported event type", nameof(displayEvent));
        }

        var isSilent = ((ExecuteRequest)request.Content).Silent;

        if (!isSilent)
        {
            // send on io
            jupyterMessageSender.Send(dataMessage);
        }
    }

    private object PreserveJson(string mimeType, string formattedValue)
    {
        var value = mimeType switch
        {
            JsonFormatter.MimeType => (object)formattedValue.FromJsonTo<JsonElement>(),
            _ => formattedValue,
        };
        return value;
    }

    private string GetPlainTextValueOrDefault(Dictionary<string, object> formattedValues, string defaultText)
    {
        if (formattedValues.TryGetValue(PlainTextFormatter.MimeType, out var text))
        {
            return text as string;
        }

        return defaultText;
    }

    private void OnCommandHandled(IJupyterMessageResponseSender jupyterMessageSender)
    {
        // reply ok
        var executeReplyPayload = new ExecuteReplyOk(executionCount: _executionCount);

        // send to server
        jupyterMessageSender.Send(executeReplyPayload);
    }
}