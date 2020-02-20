// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
        private readonly Lazy<PowerShell> _lazyPwsh;
        private CancellationTokenSource _cancellationSource;

        public PowerShellKernel()
        {
            Name = DefaultKernelName;
            _cancellationSource = new CancellationTokenSource();

            _lazyPwsh = new Lazy<PowerShell>(() => {
                //Sets the distribution channel to "PSES" so starts can be distinguished in PS7+ telemetry
                Environment.SetEnvironmentVariable("POWERSHELL_DISTRIBUTION_CHANNEL", "dotnet-interactive-powershell");

                // Create PowerShell instance
                var iss = InitialSessionState.CreateDefault();
                if(Platform.IsWindows)
                {
                    // This sets the execution policy on Windows to RemoteSigned.
                    iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;
                }

                var runspace = RunspaceFactory.CreateRunspace(iss);
                runspace.Open();
                var pwsh = PowerShell.Create(runspace);

                // Add Modules directory that contains the helper modules
                string psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
                string psJupyterModulePath = Path.Join(
                    Path.GetDirectoryName(typeof(PowerShellKernel).Assembly.Location),
                    "Modules");

                Environment.SetEnvironmentVariable("PSModulePath",
                    $"{psJupyterModulePath}{Path.PathSeparator}{psModulePath}");

                RegisterForDisposal(pwsh);
                return pwsh;
            });
        }

        protected override Task HandleSubmitCode(
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
                return Task.CompletedTask;
            }

            // Do nothing if we get a Diagnose type.
            if (submitCode.SubmissionType == SubmissionType.Diagnose)
            {
                return Task.CompletedTask;
            }

            if (cancellationSource.IsCancellationRequested)
            {
                context.Fail(null, "Command cancelled");
                return Task.CompletedTask;
            }

            StreamHandler streamHandler = RegisterPowerShellStreams(context, submitCode);
            try
            {
                _lazyPwsh.Value.AddScript(code)
                    .AddCommand(@"Microsoft.DotNet.Interactive.PowerShell\Trace-PipelineObject")
                    .InvokeAndClearCommands();
            }
            catch (Exception e)
            {
                // If a non-terminating error happened, log it and send back CommandFailed.
                // TODO: Should we even output the ErrorRecord? Maybe we should just return
                // CommandFailed?
                string stringifiedErrorRecord =
                    _lazyPwsh.Value.AddCommand(CommandUtils.OutStringCmdletInfo)
                        .AddParameter("InputObject", new ErrorRecord(e, null, ErrorCategory.NotSpecified, null))
                    .InvokeAndClearCommands<string>()[0];

                context.Fail(message: stringifiedErrorRecord);
            }
            finally
            {
                UnregisterPowerShellStreams(streamHandler);
            }
            
            return Task.CompletedTask;
        }

        protected override Task HandleRequestCompletion(
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

            return Task.CompletedTask;
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
            CommandCompletion completion = CommandCompletion.CompleteInput(code, cursorPosition, null, _lazyPwsh.Value);

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
            _lazyPwsh.Value.Streams.Debug.DataAdding += streamHandler.DebugDataAdding;
            _lazyPwsh.Value.Streams.Warning.DataAdding += streamHandler.WarningDataAdding;
            _lazyPwsh.Value.Streams.Error.DataAdding += streamHandler.ErrorDataAdding;
            _lazyPwsh.Value.Streams.Verbose.DataAdding += streamHandler.VerboseDataAdding;
            _lazyPwsh.Value.Streams.Information.DataAdding += streamHandler.InformationDataAdding;
            _lazyPwsh.Value.Streams.Progress.DataAdding += streamHandler.ProgressDataAdding;
            return streamHandler;
        }

        private void UnregisterPowerShellStreams(
            StreamHandler streamHandler)
        {
            if (streamHandler == null)
            {
                return;
            }

            _lazyPwsh.Value.Streams.Debug.DataAdding -= streamHandler.DebugDataAdding;
            _lazyPwsh.Value.Streams.Warning.DataAdding -= streamHandler.WarningDataAdding;
            _lazyPwsh.Value.Streams.Error.DataAdding -= streamHandler.ErrorDataAdding;
            _lazyPwsh.Value.Streams.Verbose.DataAdding -= streamHandler.VerboseDataAdding;
            _lazyPwsh.Value.Streams.Information.DataAdding -= streamHandler.InformationDataAdding;
            _lazyPwsh.Value.Streams.Progress.DataAdding -= streamHandler.ProgressDataAdding;
        }
    }
}
