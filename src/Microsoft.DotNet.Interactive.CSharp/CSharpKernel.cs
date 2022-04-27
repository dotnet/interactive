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
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using CompletionItem = Microsoft.DotNet.Interactive.Events.CompletionItem;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public class CSharpKernel :
        Kernel,
        IExtensibleKernel,
        ISupportNuget,
        ISupportGetValue,
        ISupportSetClrValue,
        IKernelCommandHandler<RequestCompletions>,
        IKernelCommandHandler<RequestDiagnostics>,
        IKernelCommandHandler<RequestHoverText>,
        IKernelCommandHandler<RequestSignatureHelp>,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<ChangeWorkingDirectory>
    {
        internal const string DefaultKernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        protected CSharpParseOptions _csharpParseOptions =
            new(LanguageVersion.Latest, kind: SourceCodeKind.Script);

        private InteractiveWorkspace _workspace;

        private Lazy<PackageRestoreContext> _lazyPackageRestoreContext;

        private ScriptOptions _scriptOptions;

        private readonly AssemblyBasedExtensionLoader _extensionLoader = new();
        private readonly ScriptBasedExtensionLoader _scriptExtensionLoader = new();

        private string _workingDirectory;

        public CSharpKernel() : this(DefaultKernelName)
        {

        }
        public CSharpKernel(string name) : base(name, "C#", "10.0")
        {
            _workspace = new InteractiveWorkspace();

            //For the VSCode-Add-In Directory.GetCurrentDirectory() would here return something like: c:\Users\<username>\AppData\Roaming\Code\User\globalStorage\ms-dotnettools.dotnet-interactive-vscode
            //...so we wait for RunAsync to read Directory.GetCurrentDirectory() the first time.

            _scriptOptions = ScriptOptions.Default
                         .WithLanguageVersion(LanguageVersion.Latest)
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

            _lazyPackageRestoreContext = new Lazy<PackageRestoreContext>(() =>
            {
                var packageRestoreContext = new PackageRestoreContext();

                RegisterForDisposal(packageRestoreContext);

                return packageRestoreContext;
            });

            RegisterForDisposal(() =>
            {
                _workspace.Dispose();
                _workspace = null;

                _lazyPackageRestoreContext = null;
                ScriptState = null;
                _scriptOptions = null;
            });
        }

        public ScriptState ScriptState { get; private set; }

        public Task<bool> IsCompleteSubmissionAsync(string code)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, _csharpParseOptions);
            return Task.FromResult(SyntaxFactory.IsCompleteSubmission(syntaxTree));
        }

        public IReadOnlyCollection<KernelValueInfo> GetValueInfos() =>
            ScriptState?.Variables
                       .GroupBy(v => v.Name)
                       .Select(g => new KernelValueInfo(g.Key, g.Last().Type))
                       .ToArray() ??
            Array.Empty<KernelValueInfo>();

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

        public async Task SetValueAsync(string name, object value, Type declaredType = null)
        {
            using var csharpTypeDeclaration = new StringWriter();

            declaredType ??= value.GetType();
            declaredType.WriteCSharpDeclarationTo(csharpTypeDeclaration);

            await RunAsync($"{csharpTypeDeclaration} {name} = default;");

            var scriptVariable = ScriptState.GetVariable(name);

            scriptVariable.Value = value;
        }

        public async Task HandleAsync(RequestHoverText command, KernelInvocationContext context)
        {
            await EnsureWorkspaceIsInitializedAsync(context);

            using var _ = new GCPressure(1024 * 1024);

            var document = _workspace.ForkDocumentForLanguageServices(command.Code);
            var text = await document.GetTextAsync(context.CancellationToken);
            var cursorPosition = text.Lines.GetPosition(command.LinePosition.ToCodeAnalysisLinePosition());
            var service = QuickInfoService.GetService(document);
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

        public async Task HandleAsync(RequestSignatureHelp command, KernelInvocationContext context)
        {
            await EnsureWorkspaceIsInitializedAsync(context);

            var document = _workspace.ForkDocumentForLanguageServices(command.Code);
            var signatureHelp = await SignatureHelpGenerator.GenerateSignatureInformation(document, command, context.CancellationToken);
            if (signatureHelp is { })
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

        public async Task HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
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

            if (submitCode.SubmissionType == SubmissionType.Diagnose)
            {
                return;
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

                if (kernelDiagnostics.Length > 0)
                {
                    var formattedDiagnostics =
                        diagnostics
                            .Select(d => d.ToString())
                            .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                            .ToImmutableArray();

                    context.Publish(new DiagnosticsProduced(kernelDiagnostics, submitCode, formattedDiagnostics));
                }

                // Report the compilation failure or exception
                if (exception is not null)
                {
                    context.Fail(submitCode, exception, message);
                }
                else
                {
                    if (ScriptState is not null && HasReturnValue)
                    {
                        var formattedValues = FormattedValue.FromObject(ScriptState.ReturnValue);
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

        public Task HandleAsync(ChangeWorkingDirectory command, KernelInvocationContext context)
        {
            return Task.CompletedTask;
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
                                                .UntilCancelled(cancellationToken) ?? ScriptState;
            }
            else
            {
                ScriptState = await ScriptState.ContinueWithAsync(
                                                   code,
                                                   _scriptOptions,
                                                   catchException: catchException,
                                                   cancellationToken: cancellationToken)
                                               .UntilCancelled(cancellationToken) ?? ScriptState;
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

        public async Task HandleAsync(
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
            foreach (var item in completionList.Items)
            {
                var description = await service.GetDescriptionAsync(document, item, contextCancellationToken);
                var completionItem = item.ToModel(description);
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

        public async Task HandleAsync(
            RequestDiagnostics command,
            KernelInvocationContext context)
        {
            await EnsureWorkspaceIsInitializedAsync(context);

            var document = _workspace.ForkDocumentForLanguageServices(command.Code);
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken);
            var diagnostics = semanticModel.GetDiagnostics(cancellationToken:context.CancellationToken);
            context.Publish(GetDiagnosticsProduced(command, diagnostics));
        }

        public async Task LoadExtensionsFromDirectoryAsync(
            DirectoryInfo directory,
            KernelInvocationContext context)
        {
            await _extensionLoader.LoadFromDirectoryAsync(
                directory,
                this,
                context);

            await _scriptExtensionLoader.LoadFromDirectoryAsync(
                directory,
                this,
                context);
        }

        public PackageRestoreContext PackageRestoreContext => _lazyPackageRestoreContext.Value;

        private bool HasReturnValue =>
            ScriptState is not null &&
            (bool)_hasReturnValueMethod.Invoke(ScriptState.Script, null);

        void ISupportNuget.TryAddRestoreSource(string source) => _lazyPackageRestoreContext.Value.TryAddRestoreSource(source);

        PackageReference ISupportNuget.GetOrAddPackageReference(string packageName, string packageVersion) =>
            _lazyPackageRestoreContext.Value.GetOrAddPackageReference(
                packageName,
                packageVersion);

        void ISupportNuget.RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> resolvedReferences)
        {
            var references = resolvedReferences
                             .SelectMany(r => r.AssemblyPaths)
                             .Select(r => CachingMetadataResolver.ResolveReferenceWithXmlDocumentationProvider(r))
                             .ToArray();

            foreach (var reference in references)
            {
                _workspace.AddPackageManagerReference(reference);
            }

            _scriptOptions = _scriptOptions.AddReferences(references);
        }

        Task<PackageRestoreResult> ISupportNuget.RestoreAsync() => _lazyPackageRestoreContext.Value.RestoreAsync();

        public IEnumerable<PackageReference> RequestedPackageReferences =>
            PackageRestoreContext.RequestedPackageReferences;

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences =>
            PackageRestoreContext.ResolvedPackageReferences;

        public IEnumerable<string> RestoreSources =>
            PackageRestoreContext.RestoreSources;
    }
}