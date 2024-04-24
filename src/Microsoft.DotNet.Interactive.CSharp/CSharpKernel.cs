// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp.SignatureHelp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using CompletionItem = Microsoft.DotNet.Interactive.Events.CompletionItem;

namespace Microsoft.DotNet.Interactive.CSharp;

public class CSharpKernel :
    Kernel,
    IKernelCommandHandler<RequestCompletions>,
    IKernelCommandHandler<RequestDiagnostics>,
    IKernelCommandHandler<RequestHoverText>,
    IKernelCommandHandler<RequestSignatureHelp>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>
{
    internal const string DefaultKernelName = "csharp";

    private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
        .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

    protected CSharpParseOptions _csharpParseOptions =
        new(LanguageVersion.Latest, kind: SourceCodeKind.Script);

    private InteractiveWorkspace _workspace;


    private ScriptOptions _scriptOptions;

    private string _workingDirectory;

    public CSharpKernel() : this(DefaultKernelName)
    {
    }

    public CSharpKernel(string name) : base(name)
    {
        KernelInfo.LanguageName = "C#";
        KernelInfo.LanguageVersion = "12.0";
        KernelInfo.DisplayName = $"{KernelInfo.LocalName} - C# Script";
        KernelInfo.Description = """
                                 This Kernel can compile and execute C# code and display the results.
                                 The language is C# Scripting, a dialect of C# that is used for interactive programming.
                                 """;
        _workspace = new InteractiveWorkspace();

        //For the VSCode-Add-In Directory.GetCurrentDirectory() would here return something like: c:\Users\<username>\AppData\Roaming\Code\User\globalStorage\ms-dotnettools.dotnet-interactive-vscode
        //...so we wait for RunAsync to read Directory.GetCurrentDirectory() the first time.

        _scriptOptions = ScriptOptions.Default
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .AddImports(
                "System",
                "System.Text",
                "System.Collections",
                "System.Collections.Generic",
                "System.Threading.Tasks",
                "System.Linq")
            .AddReferences(
                typeof(Enumerable).Assembly,
                typeof(IEnumerable<>).Assembly,
                typeof(Task<>).Assembly,
                typeof(Kernel).Assembly,
                typeof(CSharpKernel).Assembly,
                typeof(PocketView).Assembly);
        
        RegisterForDisposal(() =>
        {
            _workspace.Dispose();
            _workspace = null;
            ScriptState = null;
            _scriptOptions = null;
        });
    }

    public ScriptState ScriptState { get; private set; }

    private Task<bool> IsCompleteSubmissionAsync(string code)
    {
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, _csharpParseOptions);
        return Task.FromResult(SyntaxFactory.IsCompleteSubmission(syntaxTree));
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var valueInfos =
            ScriptState?.Variables
                       .GroupBy(v => v.Name)
                       .Select(g =>
                       {
                           var formattedValues = FormattedValue.CreateSingleFromObject(
                               g.LastOrDefault()?.Value,
                               command.MimeType);

                           return new KernelValueInfo(
                               g.Key,
                               formattedValues,
                               g.Last().Type);
                       })
                       .ToArray() ??
            Array.Empty<KernelValueInfo>();

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

    public bool TryGetValue<T>(
        string name,
        out T value)
    {
        object rawValue;
        bool ret;
        if (ScriptState?.Variables
                .LastOrDefault(v => v.Name == name) is { } variable)
        {
            rawValue = variable.Value;
            ret = true;
        }
        else
        {
            rawValue = default;
            ret = false;
        }
        if (ret)
        {
            value = (T)rawValue;
            return true;
        }

        value = default;
        return false;
    }

    async Task IKernelCommandHandler<SendValue>.HandleAsync(
        SendValue command,
        KernelInvocationContext context)
    {
        await SetValueAsync(command, context, SetValueAsync);
    }

    public async Task SetValueAsync(string name, object value, Type declaredType)
    {
        using var csharpTypeDeclaration = new StringWriter();

        declaredType ??= value.GetType();
        declaredType.WriteCSharpDeclarationTo(csharpTypeDeclaration);

        await RunAsync($"{csharpTypeDeclaration} {name} = default;");

        var scriptVariable = ScriptState.GetVariable(name);

        scriptVariable.Value = value;
    }

    async Task IKernelCommandHandler<RequestHoverText>.HandleAsync(RequestHoverText command, KernelInvocationContext context)
    {
        await EnsureWorkspaceIsInitializedAsync(context);

        using var _ = new GCPressure(1024 * 1024);

        var document = _workspace.ForkDocumentForLanguageServices(command.Code);
        var text = await document.GetTextAsync(context.CancellationToken);
        var cursorPosition = text.Lines.GetPosition(command.LinePosition.ToCodeAnalysisLinePosition());
        var service = QuickInfoService.GetService(document);
        if (service != null)
        {
            var info = await service.GetQuickInfoAsync(document, cursorPosition, context.CancellationToken);

            if (info is null)
            {
                return;
            }

            var scriptSpanStart = LinePosition.FromCodeAnalysisLinePosition(text.Lines.GetLinePosition(0));
            var linePosSpan = LinePositionSpan.FromCodeAnalysisLinePositionSpan(text.Lines.GetLinePositionSpan(info.Span));
            var correctedLinePosSpan = linePosSpan.SubtractLineOffset(scriptSpanStart);

            context.Publish(
                new HoverTextProduced(
                    command,
                    new[]
                    {
                        new FormattedValue("text/markdown", info.ToMarkdownString())
                    },
                    correctedLinePosSpan));
        }
    }

    async Task IKernelCommandHandler<RequestSignatureHelp>.HandleAsync(RequestSignatureHelp command, KernelInvocationContext context)
    {
        await EnsureWorkspaceIsInitializedAsync(context);

        var document = _workspace.ForkDocumentForLanguageServices(command.Code);
        var signatureHelp = await SignatureHelpGenerator.GenerateSignatureInformation(document, command, context.CancellationToken);
        if (signatureHelp is not null)
        {
            context.Publish(signatureHelp);
        }
    }

    private async Task EnsureWorkspaceIsInitializedAsync(KernelInvocationContext context)
    {
        if (ScriptState is null)
        {
            ScriptState = await CSharpScript.RunAsync(
                    string.Empty,
                    _scriptOptions,
                    cancellationToken: context.CancellationToken)
                .UntilCancelled(context.CancellationToken) ?? ScriptState;

            await _workspace.UpdateWorkspaceAsync(ScriptState);
        }
    }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        var codeSubmissionReceived = new CodeSubmissionReceived(submitCode);

        context.Publish(codeSubmissionReceived);

        var code = submitCode.Code;
        var isComplete = await IsCompleteSubmissionAsync(submitCode.Code);

        if (isComplete)
        {
            context.Publish(new CompleteCodeSubmissionReceived(submitCode));
        }
        else
        {
            context.Publish(new IncompleteCodeSubmissionReceived(submitCode));
        }

        Exception exception = null;
        string message = null;

        if (!context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(
                    code,
                    context.CancellationToken,
                    e =>
                    {
                        exception = e;
                        return true;
                    });
            }
            catch (CompilationErrorException cpe)
            {
                exception = new CodeSubmissionCompilationErrorException(cpe);
            }
            catch (Exception e)
            {
                exception = e;
            }
        }

        if (!context.CancellationToken.IsCancellationRequested)
        {
            ImmutableArray<CodeAnalysis.Diagnostic> diagnostics;

            // Check for a compilation failure
            if (exception is CodeSubmissionCompilationErrorException { InnerException: CompilationErrorException innerCompilationException })
            {
                diagnostics = innerCompilationException.Diagnostics;
                // In the case of an error the diagnostics get attached to both the
                // DiagnosticsProduced and CommandFailed events.
                message =
                    string.Join(Environment.NewLine,
                        innerCompilationException.Diagnostics.Select(d => d.ToString()));
            }
            else
            {
                diagnostics = ScriptState?.Script.GetCompilation().GetDiagnostics() ?? ImmutableArray<CodeAnalysis.Diagnostic>.Empty;
            }

            // Publish the compilation diagnostics. This doesn't include the exception.
            var kernelDiagnostics = diagnostics.Select(Diagnostic.FromCodeAnalysisDiagnostic).ToImmutableArray();

            
            var formattedDiagnostics =
                diagnostics
                    .Select(d => d.ToString())
                    .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                    .ToImmutableArray();

            context.Publish(new DiagnosticsProduced(kernelDiagnostics, submitCode, formattedDiagnostics));

            // Report the compilation failure or exception
            if (exception is not null)
            {
                context.Fail(submitCode, exception, message);
            }
            else
            {
                if (ScriptState is not null && HasReturnValue)
                {
                    IReadOnlyList<FormattedValue> formattedValues = ScriptState.ReturnValue switch
                    {
                        FormattedValue formattedValue => new[] { formattedValue },
                        IEnumerable<FormattedValue> formattedValueEnumerable => formattedValueEnumerable.ToArray(),
                        _ => FormattedValue.CreateManyFromObject(ScriptState.ReturnValue)
                    };
                    context.Publish(
                        new ReturnValueProduced(
                            ScriptState.ReturnValue,
                            submitCode,
                            formattedValues));
                }
            }
        }
        else
        {
            context.Fail(submitCode, null, "Command cancelled");
        }
    }

    private async Task RunAsync(
        string code,
        CancellationToken cancellationToken = default,
        Func<Exception, bool> catchException = default)
    {
        if (cancellationToken.IsCancellationRequested || IsDisposed)
        {
            return;
        }

        UpdateScriptOptionsIfWorkingDirectoryChanged();

        if (ScriptState is null)
        {
            ScriptState = await CSharpScript.RunAsync(
                              code,
                              _scriptOptions,
                              cancellationToken: cancellationToken)
                          ?? ScriptState;
        }
        else
        {
            ScriptState = await ScriptState.ContinueWithAsync(
                              code,
                              _scriptOptions,
                              catchException: catchException,
                              cancellationToken: cancellationToken)
                          ?? ScriptState;
        }

        if (ScriptState is not null && ScriptState.Exception is null)
        {
            await _workspace.UpdateWorkspaceAsync(ScriptState);
        }

        void UpdateScriptOptionsIfWorkingDirectoryChanged()
        {
            var currentDir = Directory.GetCurrentDirectory();

            if (!currentDir.Equals(_workingDirectory, StringComparison.Ordinal))
            {
                _workingDirectory = currentDir;

                _scriptOptions = _scriptOptions
                    .WithMetadataResolver(CachingMetadataResolver.Default.WithBaseDirectory(_workingDirectory))
                    .WithSourceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, _workingDirectory));
            }
        }
    }

    async Task IKernelCommandHandler<RequestCompletions>.HandleAsync(
        RequestCompletions command,
        KernelInvocationContext context)
    {
        await EnsureWorkspaceIsInitializedAsync(context);

        var completionList =
            await GetCompletionList(
                command.Code,
                SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition),
                context.CancellationToken);

        context.Publish(new CompletionsProduced(completionList, command));
    }

    private async Task<IEnumerable<CompletionItem>> GetCompletionList(string code,
        int cursorPosition, CancellationToken contextCancellationToken)
    {
        using var _ = new GCPressure(1024 * 1024);

        var document = _workspace.ForkDocumentForLanguageServices(code);
        var service = CompletionService.GetService(document);
        var completionList = await service.GetCompletionsAsync(document, cursorPosition, cancellationToken: contextCancellationToken);

        if (completionList is null)
        {
            return Enumerable.Empty<CompletionItem>();
        }

        var items = new List<CompletionItem>();

        foreach (CodeAnalysis.Completion.CompletionItem item in completionList.ItemsList)
        {
            // TODO: Getting a description for each item significantly slows this overall operation. We should look into caching approaches but shouldn't block completions here.
            // var description = await service.GetDescriptionAsync(document, item, contextCancellationToken);
            var completionItem = item.ToModel(CompletionDescription.Empty);
            items.Add(completionItem);
        }

        return items;
    }

    internal DiagnosticsProduced GetDiagnosticsProduced(
        KernelCommand command,
        ImmutableArray<CodeAnalysis.Diagnostic> diagnostics)
    {
        var kernelDiagnostics = diagnostics.Select(Diagnostic.FromCodeAnalysisDiagnostic).ToImmutableArray();
        var formattedDiagnostics =
            diagnostics
                .Select(d => d.ToString())
                .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                .ToImmutableArray();

        return new DiagnosticsProduced(kernelDiagnostics, command, formattedDiagnostics);
    }

    async Task IKernelCommandHandler<RequestDiagnostics>.HandleAsync(
        RequestDiagnostics command,
        KernelInvocationContext context)
    {
        await EnsureWorkspaceIsInitializedAsync(context);

        var document = _workspace.ForkDocumentForLanguageServices(command.Code);
        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken);
        var diagnostics = semanticModel.GetDiagnostics(cancellationToken: context.CancellationToken);
        if (diagnostics.Length > 0)
        {
            context.Publish(GetDiagnosticsProduced(command, diagnostics));
        }
    }

    private bool HasReturnValue =>
        ScriptState is not null &&
        (bool)_hasReturnValueMethod.Invoke(ScriptState.Script, null);
    
    public void AddAssemblyReferences(IEnumerable<string> assemblyPaths)
    {
        var references = assemblyPaths
            .Select(r => CachingMetadataResolver.ResolveReferenceWithXmlDocumentationProvider(r))
            .ToArray();

        foreach (var reference in references)
        {
            _workspace.AddPackageManagerReference(reference);
        }

        _scriptOptions = _scriptOptions.AddReferences(references);
    }
}