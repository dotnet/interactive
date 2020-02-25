// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Formatting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ExecuteRequestHandler : RequestHandlerBase<ExecuteRequest>
    {
        private int _executionCount;

        public ExecuteRequestHandler(IKernel kernel, JupyterFrontendEnvironment frontendEnvironment, IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance, frontendEnvironment)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var executeRequest = GetJupyterRequest(context);

            _executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);
            
            FrontendEnvironment.AllowStandardInput = executeRequest.AllowStdin;

            var executeInputPayload = new ExecuteInput(executeRequest.Code, _executionCount);
            context.JupyterMessageSender.Send(executeInputPayload);

            var command = new SubmitCode(executeRequest.Code);

            await SendAsync(context, command);
        }

        protected override void OnKernelEventReceived(
            IKernelEvent @event, 
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case DisplayEventBase displayEvent:
                    OnDisplayEvent(displayEvent, context.JupyterRequestMessageEnvelope, context.JupyterMessageSender);
                    break;
                case CommandHandled _:
                    OnCommandHandled(context.JupyterMessageSender);
                    break;
                case CommandFailed commandFailed:
                    OnCommandFailed(commandFailed, context.JupyterMessageSender);
                    break;
                case InputRequested inputRequested:
                    OnInputRequested(context, inputRequested);
                    break;
                case PasswordRequested passwordRequested:
                    OnPasswordRequested(context, passwordRequested);
                    break;
            }
        }

        private static Dictionary<string, object> CreateTransient(string displayId)
        {
            var transient = new Dictionary<string, object> { { "display_id", displayId ?? Guid.NewGuid().ToString() } };
            return transient;
        }

        private void OnInputRequested(JupyterRequestContext context, InputRequested inputRequested)
        {
            var inputReq = new InputRequest(inputRequested.Prompt, password: false);
            inputRequested.Content = context.JupyterMessageSender.Send(inputReq);
        }

        private void OnPasswordRequested(JupyterRequestContext context, PasswordRequested passwordRequested)
        {
            var passReq = new InputRequest(passwordRequested.Prompt, password: true);
            var clearTextPassword = context.JupyterMessageSender.Send(passReq);
            passwordRequested.Content = new PasswordString(clearTextPassword);
        }

        private void OnCommandFailed(
            CommandFailed commandFailed,
            IJupyterMessageSender jupyterMessageSender)
        {
            var traceBack = new List<string>();

            switch (commandFailed.Exception)
            {
                case CodeSubmissionCompilationErrorException _:
                    traceBack.Add(commandFailed.Message);
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

            var errorContent = new Error(
                eName: "Unhandled exception",
                eValue: commandFailed.Message,
                traceback: traceBack
            );

            // send on iopub
            jupyterMessageSender.Send(errorContent);


            //  reply Error
            var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: _executionCount);

            // send to server
            jupyterMessageSender.Send(executeReplyPayload);
        }

        private void OnDisplayEvent(DisplayEventBase displayEvent,
            ZeroMQMessage request,
            IJupyterMessageSender jupyterMessageSender)
        {
            if (displayEvent is ReturnValueProduced && displayEvent.Value is DisplayedValue)
            {
                return;
            }

            var transient = CreateTransient(displayEvent.ValueId);

            var formattedValues = displayEvent
                .FormattedValues
                .ToDictionary(k => k.MimeType, v => PreserveJson(v.MimeType, v.Value));

            var value = displayEvent.Value;
            PubSubMessage dataMessage;

            CreateDefaultFormattedValueIfEmpty(formattedValues, value);

            switch (displayEvent)
            {
                case DisplayedValueProduced _:
                    dataMessage = new DisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;

                case DisplayedValueUpdated _:
                    dataMessage = new UpdateDisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;

                case ReturnValueProduced _:
                    dataMessage = new ExecuteResult(
                        _executionCount,
                        transient: transient,
                        data: formattedValues);
                    break;

                case StandardOutputValueProduced _:
                    dataMessage = Stream.StdOut(GetPlainTextValueOrDefault(formattedValues, value?.ToString() ?? string.Empty));
                    break;

                case StandardErrorValueProduced _:
                case ErrorProduced _:
                    dataMessage = Stream.StdErr(GetPlainTextValueOrDefault(formattedValues, value?.ToString() ?? string.Empty));
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
                JsonFormatter.MimeType => (object) JToken.Parse(formattedValue),
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

        private static void CreateDefaultFormattedValueIfEmpty(Dictionary<string, object> formattedValues, object value)
        {
            if (formattedValues.Count == 0)
            {
                formattedValues.Add(
                    HtmlFormatter.MimeType,
                    value.ToDisplayString("text/html"));
            }
        }

        private void OnCommandHandled(IJupyterMessageSender jupyterMessageSender)
        {
            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: _executionCount);

            // send to server
           jupyterMessageSender.Send(executeReplyPayload);
        }
    }
}
