// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell.Host;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;
using XPlot.Plotly;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    using System.Management.Automation;

    public class PowerShellKernel : KernelBase
    {
        internal const string DefaultKernelName = "powershell";

        private static readonly CmdletInfo _outDefaultCommand;
        private static readonly PropertyInfo _writeStreamProperty;
        private static readonly object _errorStreamValue;

        private readonly PSKernelHost _psHost;
        private readonly Lazy<PowerShell> _lazyPwsh;

        public Func<string, string> ReadInput { get; set; }
        public Func<string, PasswordString> ReadPassword { get; set; }

        static PowerShellKernel()
        {
            // Prepare for marking PSObject as error with 'WriteStream'.
            _writeStreamProperty = typeof(PSObject).GetProperty("WriteStream", BindingFlags.Instance | BindingFlags.NonPublic);
            Type writeStreamType = typeof(PSObject).Assembly.GetType("System.Management.Automation.WriteStreamType");
            _errorStreamValue = Enum.Parse(writeStreamType, "Error");
            _outDefaultCommand = new CmdletInfo("Out-Default2", typeof(OutDefaultCommand));

            // Register type accelerators for Plotly.
            var accelerator = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            MethodInfo addAccelerator = accelerator.GetMethod("Add", new Type[] { typeof(string), typeof(Type) });
            foreach (Type type in typeof(Graph).GetNestedTypes())
            {
                addAccelerator.Invoke(null, new object[] { $"Graph.{type.Name}", type });
            }

            // Add accelerators that exist in other namespaces.
            addAccelerator.Invoke(null, new object[] { "Layout", typeof(Layout.Layout) });
            addAccelerator.Invoke(null, new object[] { "Chart", typeof(Chart) });
        }

        public PowerShellKernel()
        {
            Name = DefaultKernelName;
            _psHost = new PSKernelHost();
            _lazyPwsh = new Lazy<PowerShell>(CreatePowerShell);
        }

        private PowerShell CreatePowerShell()
        {
            const string PSTelemetryEnvName = "POWERSHELL_DISTRIBUTION_CHANNEL";
            const string PSTelemetryChannel = "dotnet-interactive-powershell";
            const string PSModulePathEnvName = "PSModulePath";

            // Set the distribution channel so telemetry can be distinguished in PS7+ telemetry
            Environment.SetEnvironmentVariable(PSTelemetryEnvName, PSTelemetryChannel);

            // Create PowerShell instance
            var iss = InitialSessionState.CreateDefault2();
            if(Platform.IsWindows)
            {
                // This sets the execution policy on Windows to RemoteSigned.
                iss.ExecutionPolicy = ExecutionPolicy.RemoteSigned;
            }

            var runspace = RunspaceFactory.CreateRunspace(_psHost, iss);
            runspace.Open();
            var pwsh = PowerShell.Create(runspace);

            // Add Modules directory that contains the helper modules
            string psModulePath = Environment.GetEnvironmentVariable(PSModulePathEnvName);
            string psJupyterModulePath = Path.Join(
                Path.GetDirectoryName(typeof(PowerShellKernel).Assembly.Location),
                "Modules");

            Environment.SetEnvironmentVariable(
                PSModulePathEnvName,
                $"{psJupyterModulePath}{Path.PathSeparator}{psModulePath}");

            RegisterForDisposal(pwsh);
            return pwsh;
        }

        public override bool TryGetVariable(string name, out object value)
        {
            value = null;
            return false;
        }

        protected override Task HandleSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext context)
        {
            // Acknowledge that we received the request.
            context.Publish(new CodeSubmissionReceived(submitCode));

            string code = submitCode.Code;
            PowerShell pwsh = _lazyPwsh.Value;

            // Test is the code we got is actually able to run.
            if (IsCompleteSubmission(code, out ParseError[] parseErrors))
            {
                context.Publish(new CompleteCodeSubmissionReceived(submitCode));
            }
            else
            {
                context.Publish(new IncompleteCodeSubmissionReceived(submitCode));
            }

            // If there were parse errors, display them and return early.
            if (parseErrors.Length > 0)
            {
                var parseException = new ParseException(parseErrors);
                ReportError(parseException.ErrorRecord, pwsh);
                return Task.CompletedTask;
            }

            // Do nothing if we get a Diagnose type.
            if (submitCode.SubmissionType == SubmissionType.Diagnose)
            {
                return Task.CompletedTask;
            }

            if (context.CancellationToken.IsCancellationRequested)
            {
                context.Fail(null, "Command cancelled");
                return Task.CompletedTask;
            }

            try
            {
                pwsh.AddScript(code)
                    .AddCommand(_outDefaultCommand)
                    .Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

                pwsh.InvokeAndClearCommands();
            }
            catch (Exception e)
            {
                var error = e is IContainsErrorRecord icer
                    ? icer.ErrorRecord
                    : new ErrorRecord(e, "JupyterPSHost.ReportException", ErrorCategory.NotSpecified, targetObject: null);

                ReportError(error, pwsh);
            }
            finally
            {
                ((PSKernelHostUserInterface)_psHost.UI).ResetProgress();
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

        private void ReportError(ErrorRecord error, PowerShell pwsh)
        {
            var psObject = PSObject.AsPSObject(error);
            _writeStreamProperty.SetValue(psObject, _errorStreamValue);

            pwsh.AddCommand(_outDefaultCommand)
                .AddParameter("InputObject", psObject)
                .InvokeAndClearCommands();
        }
    }
}
