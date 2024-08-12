// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.PowerShell.Host;
using Microsoft.DotNet.Interactive.ValueSharing;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

namespace Microsoft.DotNet.Interactive.PowerShell;

using System.Management.Automation;

using Microsoft.DotNet.Interactive.Utility;

public class PowerShellKernel :
    Kernel,
    IKernelCommandHandler<RequestCompletions>,
    IKernelCommandHandler<RequestDiagnostics>,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>
{
    private const string PSTelemetryEnvName = "POWERSHELL_DISTRIBUTION_CHANNEL";
    private const string PSTelemetryChannel = "dotnet-interactive-powershell";
    private const string PSModulePathEnvName = "PSModulePath";

    internal const string DefaultKernelName = "pwsh";
    internal const string LanguageName = "PowerShell";

    private static readonly CmdletInfo _outDefaultCommand;
    private static readonly PropertyInfo _writeStreamProperty;
    private static readonly object _errorStreamValue;
    private static readonly MethodInfo _addAccelerator;

    private readonly PSKernelHost _psHost;
    private readonly Lazy<PowerShell> _lazyPwsh;

    internal PowerShell Pwsh => _lazyPwsh.Value;

    public Func<string, string> ReadInput { get; set; }

    public Func<string, PasswordString> ReadPassword { get; set; }

    internal AzShellConnectionUtils AzShell { get; set; }

    internal int DefaultRunspaceId => _lazyPwsh.IsValueCreated ? Pwsh.Runspace.Id : -1;

    private readonly HashSet<string> _suppressedValueInfoNames = new();

    static PowerShellKernel()
    {
        // Prepare for marking PSObject as error with 'WriteStream'.
        _writeStreamProperty = typeof(PSObject).GetProperty("WriteStream", BindingFlags.Instance | BindingFlags.NonPublic);
        var writeStreamType = typeof(PSObject).Assembly.GetType("System.Management.Automation.WriteStreamType");
        _errorStreamValue = Enum.Parse(writeStreamType, "Error");

        // When the downstream cmdlet of a native executable is 'Out-Default', PowerShell assumes
        // it's running in the console where the 'Out-Default' would be added by default. Hence,
        // PowerShell won't redirect the standard output of the executable.
        // To workaround that, we rename 'Out-Default' to 'Out-Default2' to make sure the standard
        // output is captured.
        _outDefaultCommand = new CmdletInfo("Out-Default2", typeof(OutDefaultCommand));

        // Get the AddAccelerator method
        var acceleratorType = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
        _addAccelerator = acceleratorType?.GetMethod("Add", [typeof(string), typeof(Type)]);
    }

    public PowerShellKernel() : this(DefaultKernelName)
    {
    }

    public PowerShellKernel(string name) : base(name)
    {
        KernelInfo.LanguageName = LanguageName;
        KernelInfo.LanguageVersion = "7";
        KernelInfo.Description = """
                                 This Kernel can evaluate Powershell scripts and commands. 
                                 It uses Powershell Core and can interop with the operative system and host machine.
                                 """;

        _psHost = new PSKernelHost(this);
        _lazyPwsh = new Lazy<PowerShell>(CreatePowerShell);

        var psObject = Pwsh.Runspace.SessionStateProxy.InvokeProvider.Item.Get("variable:")?.FirstOrDefault();

        if (psObject?.BaseObject is Dictionary<string, PSVariable>.ValueCollection valueCollection)
        {
            _suppressedValueInfoNames.UnionWith(valueCollection.Select(x => x.Name));
        }
    }

    private PowerShell CreatePowerShell()
    {
        // Set the distribution channel so telemetry can be distinguished in PS7+ telemetry
        Environment.SetEnvironmentVariable(PSTelemetryEnvName, PSTelemetryChannel);

        // Create PowerShell instance
        var iss = InitialSessionState.CreateDefault2();
        if (Platform.IsWindows)
        {
            // This sets the execution policy on Windows to RemoteSigned.
            iss.ExecutionPolicy = ExecutionPolicy.RemoteSigned;
        }

        // Set $PROFILE.
        var profileValue = DollarProfileHelper.GetProfileValue();
        iss.Variables.Add(new SessionStateVariableEntry("PROFILE", profileValue, "The $PROFILE."));

        var runspace = RunspaceFactory.CreateRunspace(_psHost, iss);
        runspace.Open();
        var pwsh = PowerShell.Create(runspace);

        // Add Modules directory that contains the helper modules
        var psJupyterModulePath = Path.Join(
            Path.GetDirectoryName(typeof(PowerShellKernel).Assembly.Location),
            "Modules");

        AddModulePath(psJupyterModulePath);
        RegisterForDisposal(pwsh);
        return pwsh;
    }

    public void AddModulePath(string modulePath)
    {
        if (string.IsNullOrWhiteSpace(modulePath))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(modulePath));
        }

        var psModulePath = Environment.GetEnvironmentVariable(PSModulePathEnvName);

        Environment.SetEnvironmentVariable(
            PSModulePathEnvName,
            $"{modulePath}{Path.PathSeparator}{psModulePath}");
    }

    public void AddAccelerator(string name, Type type)
    {
        _addAccelerator?.Invoke(null, [name, type]);
    }

    public bool TryGetValue<T>(string name, out T value)
    {
        var variable = Pwsh.Runspace.SessionStateProxy.PSVariable.Get(name);

        if (variable is not null)
        {
            object outVal = (variable.Value is PSObject psobject) ? psobject.Unwrap() : variable.Value;

            if (outVal is T tObj)
            {
                value = tObj;
                return true;
            }
        }

        value = default;
        return false;
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var psObject = Pwsh.Runspace.SessionStateProxy.InvokeProvider.Item.Get("variable:")?.FirstOrDefault();

        KernelValueInfo[] valueInfos;

        if (psObject?.BaseObject is Dictionary<string, PSVariable>.ValueCollection valueCollection)
        {
            valueInfos = valueCollection
                         .Where(v => !_suppressedValueInfoNames.Contains(v.Name))
                         .Select(v =>
                         {
                             var formattedValue = FormattedValue.CreateSingleFromObject(
                                 v.Value,
                                 command.MimeType);

                             return new KernelValueInfo(
                                 v.Name,
                                 formattedValue,
                                 v.Value?.GetType());
                         }).ToArray();
        }
        else
        {
            valueInfos = Array.Empty<KernelValueInfo>();
        }

        context.Publish(new ValueInfosProduced(valueInfos, command));

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (TryGetValue<object>(command.Name, out var value))
        {
            context.PublishValueProduced(command, value);
        }
        else
        {
            context.Fail(command, message: $"Value '{command.Name}' not found in kernel {Name}");
        }

        return Task.CompletedTask;
    }

    async Task IKernelCommandHandler<SendValue>.HandleAsync(
        SendValue command,
        KernelInvocationContext context)
    {
        await SetValueAsync(command, context, SetAsync);

        Task SetAsync(string name, object value, Type declaredType)
        {
            _lazyPwsh.Value.Runspace.SessionStateProxy.PSVariable.Set(name, value);
            return Task.CompletedTask;
        }
    }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode submitCode,
        KernelInvocationContext context)
    {
        // Acknowledge that we received the request.
        context.Publish(new CodeSubmissionReceived(submitCode));

        var code = submitCode.Code;

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
            var formattedDiagnostics =
                parseErrors
                    .Select(d => d.ToString())
                    .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                    .ToImmutableArray();

            var diagnostics = parseErrors.Select(ToDiagnostic).ToImmutableArray();
            context.Publish(new DiagnosticsProduced(diagnostics, formattedDiagnostics, submitCode));

            var parseException = new ParseException(parseErrors);
            ReportError(parseException.ErrorRecord);
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            context.Fail(submitCode, null, "Command cancelled");
            return;
        }

        if (AzShell is not null)
        {
            await RunInAzShell(code);
        }
        else
        {
            var success = RunLocally(code, out var errorMessage);

            if (!success)
            {
                context.Fail(context.Command, message: errorMessage);
            }
        }
    }

    Task IKernelCommandHandler<RequestCompletions>.HandleAsync(
        RequestCompletions requestCompletions,
        KernelInvocationContext context)
    {
        CompletionsProduced completion;

        if (AzShell is not null)
        {
            // Currently no tab completion when interacting with AzShell.
            completion = new CompletionsProduced(Array.Empty<CompletionItem>(), requestCompletions);
        }
        else
        {
            var results = CommandCompletion.CompleteInput(
                requestCompletions.Code,
                SourceUtilities.GetCursorOffsetFromPosition(requestCompletions.Code, requestCompletions.LinePosition),
                options: null,
                Pwsh);

            var completionItems = results.CompletionMatches.Select(
                c => new CompletionItem(
                    displayText: c.CompletionText,
                    kind: c.ResultType.ToString(),
                    documentation: c.ToolTip));

            // The end index is the start index plus the length of the replacement.
            var endIndex = results.ReplacementIndex + results.ReplacementLength;
            completion = new CompletionsProduced(
                completionItems.ToArray(),
                requestCompletions,
                SourceUtilities.GetLinePositionSpanFromStartAndEndIndices(requestCompletions.Code, results.ReplacementIndex, endIndex));
        }

        context.Publish(completion);
        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestDiagnostics>.HandleAsync(
        RequestDiagnostics requestDiagnostics,
        KernelInvocationContext context)
    {
        var code = requestDiagnostics.Code;

        IsCompleteSubmission(code, out var parseErrors);
        
        var diagnostics = parseErrors.Select(ToDiagnostic).ToArray();
        context.Publish(new DiagnosticsProduced(
                            diagnostics,   
                            diagnostics.Select(d => new FormattedValue(PlainTextFormatter.MimeType, d.ToString())).ToArray(), 
                            requestDiagnostics));

        return Task.CompletedTask;
    }

    private async Task RunInAzShell(string code)
    {
        code = code.Trim();
        var shouldDispose = false;

        try
        {
            if (string.Equals(code, "exit", StringComparison.OrdinalIgnoreCase))
            {
                await AzShell.ExitSession();
                shouldDispose = true;
            }
            else
            {
                await AzShell.SendCommand(code);
            }
        }
        catch (IOException e)
        {
            ReportException(e);
            shouldDispose = true;
        }

        if (shouldDispose)
        {
            AzShell.Dispose();
            AzShell = null;
        }
    }

    internal bool RunLocally(string code, out string errorMessage)
    {
        var command = new Command(code, isScript: true);

        var succeeded = true;
        errorMessage = "";

        try
        {
            Pwsh.Commands.AddCommand(command);
            Pwsh.AddCommand(_outDefaultCommand);

            Pwsh.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

            Pwsh.InvokeAndClear();

            Pwsh.AddScript(
                """
                $error
                $error.clear()
                """);
            var errors = new List<string>();
            try
            {
                Pwsh.Invoke(input: null, output: errors);
            }
            finally
            {
                Pwsh.Clear();
            }

            if (errors.Count > 0)
            {
                succeeded = false;
                errorMessage = string.Join(Environment.NewLine, errors);
            }
        }
        catch (Exception e)
        {
            ReportException(e);
            succeeded = false;
        }
        finally
        {
            ((PSKernelHostUserInterface)_psHost.UI).ResetProgress();
        }

        return succeeded;
    }

    private static bool IsCompleteSubmission(string code, out ParseError[] errors)
    {
        // Parse the PowerShell script. If there are any parse errors, check if the input was incomplete.
        // We only need to check if the first ParseError has incomplete input. This is consistant with
        // what PowerShell itself does today.
        Parser.ParseInput(code, out _, out errors);
        return errors.Length == 0 || !errors[0].IncompleteInput;
    }

    private void ReportError(ErrorRecord error)
    {
        var psObject = PSObject.AsPSObject(error);
        _writeStreamProperty.SetValue(psObject, _errorStreamValue);

        Pwsh.AddCommand(_outDefaultCommand)
            .AddParameter("InputObject", psObject)
            .InvokeAndClear();
    }

    private void ReportException(Exception e)
    {
        var error = e is IContainsErrorRecord icer
            ? icer.ErrorRecord
            : new ErrorRecord(e, "JupyterPSHost.ReportException", ErrorCategory.NotSpecified, targetObject: null);

        ReportError(error);
    }

    private static Diagnostic ToDiagnostic(ParseError parseError)
    {
        return new Diagnostic(
            new LinePositionSpan(
                new LinePosition(parseError.Extent.StartLineNumber - 1, parseError.Extent.StartColumnNumber),
                new LinePosition(parseError.Extent.EndLineNumber - 1, parseError.Extent.EndColumnNumber)),
            DiagnosticSeverity.Error,
            parseError.ErrorId,
            parseError.Message);
    }
}