﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell.Commands;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    using System.Management.Automation;

    public class PowerShellKernel : KernelBase
    {
        internal const string DefaultKernelName = "powershell";

        private readonly object _cancellationSourceLock = new object();
        private readonly PowerShell _pwsh;
        private CancellationTokenSource _cancellationSource;

        public PowerShellKernel()
        {
            //Sets the distribution channel to "PSES" so starts can be distinguished in PS7+ telemetry
            Environment.SetEnvironmentVariable("POWERSHELL_DISTRIBUTION_CHANNEL", "dotnet-interactive-powershell");

            var runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault());
            runspace.Open();
            _pwsh = PowerShell.Create(runspace);
            _cancellationSource = new CancellationTokenSource();
            Name = DefaultKernelName;

            // Add Modules directory that contains the helper modules
            string psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
            string psJupyterModulePath = Path.Join(
                Path.GetDirectoryName(typeof(PowerShellKernel).Assembly.Location),
                "Modules");

            Environment.SetEnvironmentVariable("PSModulePath",
                $"{psJupyterModulePath}{Path.PathSeparator}{psModulePath}");
        }

        protected override Task HandleAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            if (command is KernelCommandBase kb)
            {
                if (kb.Handler == null)
                {
                    switch (command)
                    {
                        case SubmitCode submitCode:
                            submitCode.Handler = (_, invocationContext) =>
                            {
                                HandleSubmitCode(submitCode, context);
                                return Task.CompletedTask;
                            };
                            break;

                        case RequestCompletion requestCompletion:
                            requestCompletion.Handler = (_, invocationContext) =>
                            {
                                HandleRequestCompletion(requestCompletion, invocationContext);
                                return Task.CompletedTask;
                            };
                            break;

                        case CancelCurrentCommand interruptExecution:
                            interruptExecution.Handler = (_, invocationContext) =>
                            {
                                HandleCancelCurrentCommand(interruptExecution, invocationContext);
                                return Task.CompletedTask;
                            };
                            break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        private void HandleSubmitCode(
                SubmitCode submitCode,
                KernelInvocationContext context)
        {
            CancellationTokenSource cancellationSource;
            lock (_cancellationSourceLock)
            {
                cancellationSource = _cancellationSource;
            }

            // Acknowledge that we received the request.
            var codeSubmissionReceived = new CodeSubmissionReceived(submitCode);
            context.Publish(codeSubmissionReceived);

            // Test is the code we got is actually able to run.
            var code = submitCode.Code;
            if (IsCompleteSubmission(code, out ParseError[] parseErrors))
            {
                context.Publish(new CompleteCodeSubmissionReceived(submitCode));
            }
            else
            {
                context.Publish(new IncompleteCodeSubmissionReceived(submitCode));
            }

            // If there were parse errors, publish them and return early.
            if (parseErrors.Length > 0)
            {
                context.Fail(message: string.Join(Environment.NewLine + Environment.NewLine,
                    parseErrors.Select(pe => pe.ToString())));
                return;
            }

            // Do nothing if we get a Diagnose type.
            if (submitCode.SubmissionType == SubmissionType.Diagnose)
            {
                return;
            }

            if (cancellationSource.IsCancellationRequested)
            {
                context.Fail(null, "Command cancelled");
                return;
            }

            StreamHandler streamHandler = RegisterPowerShellStreams(context, submitCode);
            try
            {
                _pwsh.AddScript(code)
                    .AddCommand(@"Microsoft.DotNet.Interactive.PowerShell\Trace-PipelineObject")
                    .InvokeAndClearCommands();
            }
            catch (Exception e)
            {
                // If a non-terminating error happened, log it and send back CommandFailed.
                // TODO: Should we even output the ErrorRecord? Maybe we should just return
                // CommandFailed?
                string stringifiedErrorRecord =
                    _pwsh.AddCommand(CommandUtils.OutStringCmdletInfo)
                        .AddParameter("InputObject", new ErrorRecord(e, null, ErrorCategory.NotSpecified, null))
                    .InvokeAndClearCommands<string>()[0];

                context.Fail(message: stringifiedErrorRecord);
            }
            finally
            {
                UnregisterPowerShellStreams(streamHandler);
            }
        }

        private void HandleRequestCompletion(
            RequestCompletion requestCompletion,
            KernelInvocationContext context)
        {
            var completionRequestReceived = new CompletionRequestReceived(requestCompletion);

            context.Publish(completionRequestReceived);

            var completionList =
                GetCompletionList(
                    requestCompletion.Code,
                    requestCompletion.CursorPosition);

            context.Publish(new CompletionRequestCompleted(completionList, requestCompletion));
        }

        private void HandleCancelCurrentCommand(
            CancelCurrentCommand cancelCurrentCommand,
            KernelInvocationContext context)
        {
            lock (_cancellationSourceLock)
            {
                _cancellationSource.Cancel();
                _cancellationSource = new CancellationTokenSource();
                if (_pwsh.Runspace.RunspaceAvailability != RunspaceAvailability.Available)
                {
                    _pwsh.Stop();
                }
            }

            var reply = new CurrentCommandCancelled(cancelCurrentCommand);
            context.Publish(reply);
        }

        public static bool IsCompleteSubmission(string code, out ParseError[] errors)
        {
            // Parse the PowerShell script. If there are any parse errors, check if the input was incomplete.
            // We only need to check if the first ParseError has incomplete input. This is consistant with
            // what PowerShell itself does today.
            Parser.ParseInput(code, out Token[] tokens, out errors);
            return errors.Length == 0 || !errors[0].IncompleteInput;
        }

        private IEnumerable<CompletionItem> GetCompletionList(
            string code,
            int cursorPosition)
        {
            CommandCompletion completion = CommandCompletion.CompleteInput(code, cursorPosition, null, _pwsh);

            return completion.CompletionMatches.Select(c => new CompletionItem(
                displayText: c.CompletionText,
                kind: c.ResultType.ToString(),
                documentation: c.ToolTip
            ));
        }

        private StreamHandler RegisterPowerShellStreams(
            KernelInvocationContext context,
            IKernelCommand command)
        {
            var streamHandler = new StreamHandler(context, command);
            _pwsh.Streams.Debug.DataAdding += streamHandler.DebugDataAdding;
            _pwsh.Streams.Warning.DataAdding += streamHandler.WarningDataAdding;
            _pwsh.Streams.Error.DataAdding += streamHandler.ErrorDataAdding;
            _pwsh.Streams.Verbose.DataAdding += streamHandler.VerboseDataAdding;
            _pwsh.Streams.Information.DataAdding += streamHandler.InformationDataAdding;
            _pwsh.Streams.Progress.DataAdding += streamHandler.ProgressDataAdding;
            return streamHandler;
        }

        private void UnregisterPowerShellStreams(
            StreamHandler streamHandler)
        {
            if (streamHandler == null)
            {
                return;
            }

            _pwsh.Streams.Debug.DataAdding -= streamHandler.DebugDataAdding;
            _pwsh.Streams.Warning.DataAdding -= streamHandler.WarningDataAdding;
            _pwsh.Streams.Error.DataAdding -= streamHandler.ErrorDataAdding;
            _pwsh.Streams.Verbose.DataAdding -= streamHandler.VerboseDataAdding;
            _pwsh.Streams.Information.DataAdding -= streamHandler.InformationDataAdding;
            _pwsh.Streams.Progress.DataAdding -= streamHandler.ProgressDataAdding;
        }
    }
}
