// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    public class PowerShellKernel : KernelBase, IExtensibleKernel
    {
        internal const string DefaultKernelName = "powershell";

        private Runspace _runspace;
        private System.Management.Automation.PowerShell _pwsh;
        private SemaphoreSlim _runspaceSemaphore;
        private CancellationTokenSource _cancellationSource;
        private readonly object _cancellationSourceLock = new object();

        public PowerShellKernel()
        {
            _runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault());
            _runspace.Open();
            _pwsh = System.Management.Automation.PowerShell.Create(_runspace);
            _runspaceSemaphore = new SemaphoreSlim(1, 1);
            _cancellationSource = new CancellationTokenSource();
            Name = DefaultKernelName;

            string psModulePath = Environment.GetEnvironmentVariable("PSModulePath");

            Environment.SetEnvironmentVariable("PSModulePath",
                psModulePath + Path.PathSeparator + Path.Join(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Modules"));
        }

        #region Overrides

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
                            submitCode.Handler = async (_, invocationContext) =>
                            {
                                await HandleSubmitCode(submitCode, context);
                            };
                            break;

                        case RequestCompletion requestCompletion:
                            requestCompletion.Handler = async (_, invocationContext) =>
                            {
                                await HandleRequestCompletion(requestCompletion, invocationContext);
                            };
                            break;

                        case CancelCurrentCommand interruptExecution:
                            interruptExecution.Handler = async (_, invocationContext) =>
                            {
                                await HandleCancelCurrentCommand(interruptExecution, invocationContext);
                            };
                            break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Handlers

        private async Task HandleSubmitCode(
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
            if (IsCompleteSubmission(code))
            {
                context.Publish(new CompleteCodeSubmissionReceived(submitCode));
            }
            else
            {
                context.Publish(new IncompleteCodeSubmissionReceived(submitCode));
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
        
            // Wait until we have access to the runspace since we only can run one cell at a time.
            await _runspaceSemaphore.WaitAsync();

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
                    _pwsh.AddCommand(@"Microsoft.PowerShell.Utility\Out-String")
                        .AddParameter("InputObject", new ErrorRecord(e, null, ErrorCategory.NotSpecified, null))
                    .InvokeAndClearCommands<string>()[0];

                StreamHandler.PublishStreamRecord(stringifiedErrorRecord, context, submitCode);
                context.Publish(new CommandFailed(e, submitCode, e.Message));
            }
            finally
            {
                UnregisterPowerShellStreams(streamHandler);
                _runspaceSemaphore.Release();
            }
        }

        private async Task HandleRequestCompletion(
            RequestCompletion requestCompletion,
            KernelInvocationContext context)
        {
            var completionRequestReceived = new CompletionRequestReceived(requestCompletion);

            context.Publish(completionRequestReceived);

            var completionList =
                await GetCompletionList(
                    requestCompletion.Code, 
                    requestCompletion.CursorPosition);

            context.Publish(new CompletionRequestCompleted(completionList, requestCompletion));
        }

        private Task HandleCancelCurrentCommand(
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

            return Task.CompletedTask;
        }

        #endregion

        public bool IsCompleteSubmission(string code)
        {
            // Parse the PowerShell script. If there are any parse errors, then we return false.
            Parser.ParseInput(code, out Token[] tokens, out ParseError[] errors);
            return errors.Length > 0;
        }

        private async Task<IEnumerable<CompletionItem>> GetCompletionList(
            string code,
            int cursorPosition)
        {
            await _runspaceSemaphore.WaitAsync();

            try
            {
                // using (var ps = System.Management.Automation.PowerShell.Create(_runspace))
                // {
                CommandCompletion completion = CommandCompletion.CompleteInput(code, cursorPosition, null, _pwsh);

                return completion.CompletionMatches.Select(c => new CompletionItem(
                    displayText: c.CompletionText,
                    kind: c.ResultType.ToString(),
                    documentation: c.ToolTip
                ));
                // }
            }
            finally
            {
                _runspaceSemaphore.Release();
            }
        }

        public async Task LoadExtensionsFromDirectory(
            DirectoryInfo directory,
            KernelInvocationContext context)
        {
            var extensionsDirectory =
                new DirectoryInfo(
                    Path.Combine(
                        directory.FullName,
                        "interactive-extensions",
                        "dotnet",
                        "cs"));

            await new KernelExtensionAssemblyLoader().LoadFromAssembliesInDirectory(
                extensionsDirectory,
                context.HandlingKernel,
                context);
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
        }
    }
}
