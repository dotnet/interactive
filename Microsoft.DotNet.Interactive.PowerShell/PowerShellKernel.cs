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
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    public class PowerShellKernel : KernelBase, IExtensibleKernel
    {
        internal const string DefaultKernelName = "powershell";

        private Runspace _runspace;
        private SemaphoreSlim _runspaceSemaphore;
        private CancellationTokenSource _cancellationSource;
        private readonly object _cancellationSourceLock = new object();

        public PowerShellKernel()
        {
            var iss = InitialSessionState.CreateDefault();
            _runspace = RunspaceFactory.CreateRunspace(iss);
            _runspace.Open();
            _runspaceSemaphore = new SemaphoreSlim(1, 1);
            _cancellationSource = new CancellationTokenSource();
            Name = DefaultKernelName;

            string psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
            Environment.SetEnvironmentVariable("PSModulePath",
                psModulePath + Path.PathSeparator +
                System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Modules");
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
            
            var codeSubmissionReceived = new CodeSubmissionReceived(submitCode);

            context.Publish(codeSubmissionReceived);

            var code = submitCode.Code;
            if (IsCompleteSubmission(code))
            {
                context.Publish(new CompleteCodeSubmissionReceived(submitCode));
            }
            else
            {
                context.Publish(new IncompleteCodeSubmissionReceived(submitCode));
            }

            if (submitCode.SubmissionType == SubmissionType.Diagnose)
            {
                return;
            }

            Exception exception = null;
            if (!cancellationSource.IsCancellationRequested)
            {
                await _runspaceSemaphore.WaitAsync();
                try
                {
                    using(var ps = System.Management.Automation.PowerShell.Create(_runspace))
                    {
                        RegisterPowerShellStreams(ps, context, submitCode);
                        try
                        {
                            ps.AddScript(code)
                                .AddCommand(@"Microsoft.DotNet.Interactive.PowerShell\Trace-PipelineObject")
                                .InvokeAndClearCommands();
                        }
                        catch (Exception e)
                        {
                            string stringifiedErrorRecord =
                                ps.AddCommand(@"Out-String")
                                    .AddParameter("InputObject", new ErrorRecord(e, null, ErrorCategory.NotSpecified, null))
                                .InvokeAndClearCommands<string>()[0];

                            StreamHandler.PublishStreamRecord(stringifiedErrorRecord, context, submitCode);
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    _runspaceSemaphore.Release();
                }
            }

            if (!cancellationSource.IsCancellationRequested)
            {
                if (exception != null)
                {
                    string message = null;
                    context.Publish(new CommandFailed(exception, submitCode, message));
                }
            }
            else
            {
                context.Publish(new CommandFailed(null, submitCode, "Command cancelled"));
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
            var reply = new CurrentCommandCancelled(cancelCurrentCommand);
            lock (_cancellationSourceLock)
            {
                _cancellationSource.Cancel();
                _cancellationSource = new CancellationTokenSource();
            }

            context.Publish(reply);

            return Task.CompletedTask;
        }

        #endregion

        public bool IsCompleteSubmission(string code)
        {
            Parser.ParseInput(code, out Token[] tokens, out ParseError[] errors);
            return errors.Length > 0;
        }

        private void PublishOutput(
            string output,
            KernelInvocationContext context,
            IKernelCommand command)
        {
            var formattedValues = new List<FormattedValue>
                        {
                            new FormattedValue(
                                PlainTextFormatter.MimeType, output)
                        };

            context.Publish(
                new StandardOutputValueProduced(
                    output,
                    command,
                    formattedValues));
        }

        private void PublishStreamRecord(
            object output,
            KernelInvocationContext context,
            IKernelCommand command)
        {
            context.Publish(
                new DisplayedValueProduced(
                    output,
                    command,
                    FormattedValue.FromObject(output)));
        }

        private void PublishError(
            string error,
            KernelInvocationContext context,
            IKernelCommand command)
        {
            var formattedValues = new List<FormattedValue>
            {
                new FormattedValue(
                    PlainTextFormatter.MimeType, error)
            };
            
            context.Publish(
                new StandardErrorValueProduced(
                    error,
                    command,
                    formattedValues));
        }

        private async Task<IEnumerable<CompletionItem>> GetCompletionList(
            string code,
            int cursorPosition)
        {
            await _runspaceSemaphore.WaitAsync();

            try
            {
                using (var ps = System.Management.Automation.PowerShell.Create(_runspace))
                {
                    CommandCompletion completion = CommandCompletion.CompleteInput(code, cursorPosition, null, ps);

                    return completion.CompletionMatches.Select(c => new CompletionItem(
                        displayText: c.CompletionText,
                        kind: c.ResultType.ToString(),
                        documentation: c.ToolTip
                    ));
                }
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

        private void RegisterPowerShellStreams(
            System.Management.Automation.PowerShell powerShell,
            KernelInvocationContext context,
            IKernelCommand command)
        {
            var streamHandler = new StreamHandler(context, command);
            powerShell.Streams.Debug.DataAdding += streamHandler.DebugDataAdding;
            powerShell.Streams.Warning.DataAdding += streamHandler.WarningDataAdding;
            powerShell.Streams.Error.DataAdding += streamHandler.ErrorDataAdding;
            powerShell.Streams.Verbose.DataAdding += streamHandler.VerboseDataAdding;
            powerShell.Streams.Information.DataAdding += streamHandler.InformationDataAdding;
        }
    }
}
