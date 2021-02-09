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


using CompletionItem = Microsoft.DotNet.Interactive.Events.CompletionItem;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public class CSharpKernel :
        DotNetKernel,
        IExtensibleKernel,
        ISupportNuget,
        IKernelCommandHandler<RequestCompletions>,
        IKernelCommandHandler<RequestDiagnostics>,
        IKernelCommandHandler<RequestHoverText>,
        IKernelCommandHandler<RequestSignatureHelp>,
        IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        protected CSharpParseOptions _csharpParseOptions =
            new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);

        private InteractiveWorkspace _workspace;

        private Lazy<PackageRestoreContext> _packageRestoreContext;

        internal ScriptOptions ScriptOptions =
            ScriptOptions.Default
                         .WithMetadataResolver(CachingMetadataResolver.Default.WithBaseDirectory(Directory.GetCurrentDirectory()))
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

        private readonly AssemblyBasedExtensionLoader _extensionLoader = new AssemblyBasedExtensionLoader();
        private string _currentDirectory;

        public CSharpKernel() : base(DefaultKernelName)
        {
            _workspace = new InteractiveWorkspace();
            _currentDirectory = Directory.GetCurrentDirectory();

            _packageRestoreContext = new Lazy<PackageRestoreContext>(() =>
            {
                var packageRestoreContext = new PackageRestoreContext();

                RegisterForDisposal(packageRestoreContext);

                return packageRestoreContext;
            });

            RegisterForDisposal(() =>
            {
                _workspace.Dispose();
                _workspace = null;

                _packageRestoreContext = null;
                ScriptState = null;
                ScriptOptions = null;
            });
        }

        public ScriptState ScriptState { get; private set; }

        public Task<bool> IsCompleteSubmissionAsync(string code)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, _csharpParseOptions);
            return Task.FromResult(SyntaxFactory.IsCompleteSubmission(syntaxTree));
        }

        public override IReadOnlyCollection<string> GetVariableNames() =>
            ScriptState?.Variables
                       .Select(v => v.Name)
                       .Distinct()
                       .ToArray() ??
            Array.Empty<string>();

        public override bool TryGetVariable<T>(
            string name,
            out T value)
        {
            if (ScriptState?.Variables
                           .LastOrDefault(v => v.Name == name) is { } variable)
            {
                value = (T)variable.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override async Task SetVariableAsync(string name, object value, Type declaredType = null)
        {
            var csharpTypeDeclaration = new StringWriter();

            declaredType ??= value.GetType();
            declaredType.WriteCSharpDeclarationTo(csharpTypeDeclaration);

            await RunAsync($"{csharpTypeDeclaration} {name} = default;");

            var scriptVariable = ScriptState.GetVariable(name);

            scriptVariable.Value = value;
        }

        public async Task HandleAsync(RequestHoverText command, KernelInvocationContext context)
        {
            using var _ = new GCPressure(1024 * 1024);

            var document = _workspace.UpdateWorkingDocument(command.Code);
            var text = await document.GetTextAsync();
            var cursorPosition = text.Lines.GetPosition(command.LinePosition.ToCodeAnalysisLinePosition());
            var service = QuickInfoService.GetService(document);
            var info = await service.GetQuickInfoAsync(document, cursorPosition);
            
            if (info == null)
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
            var document = _workspace.UpdateWorkingDocument(command.Code);
            var signatureHelp = await SignatureHelpGenerator.GenerateSignatureInformation(document, command);
            if (signatureHelp is { })
            {
                context.Publish(signatureHelp);
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
                var diagnostics = ImmutableArray<CodeAnalysis.Diagnostic>.Empty;

                // Check for a compilation failure
                if (exception is CodeSubmissionCompilationErrorException compilationError &&
                    compilationError.InnerException is CompilationErrorException innerCompilationException)
                {
                    diagnostics = innerCompilationException.Diagnostics;
                    // In the case of an error the diagnostics get attached to both the 
                    // DiagnosticsProduced and CommandFailed events.
                    message =
                        string.Join(Environment.NewLine,
                                    innerCompilationException.Diagnostics.Select(d => d.ToString()) ?? Enumerable.Empty<string>());
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
                if (exception != null)
                {
                    context.Fail(exception, message);
                }
                else
                {
                    if (ScriptState != null && HasReturnValue)
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
                context.Fail(null, "Command cancelled");
            }
        }

        private async Task RunAsync(
            string code,
            CancellationToken cancellationToken = default,
            Func<Exception, bool> catchException = default)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (_currentDirectory != currentDirectory)
            {
                _currentDirectory = currentDirectory;
                ScriptOptions = ScriptOptions.WithMetadataResolver(
                    CachingMetadataResolver.Default.WithBaseDirectory(
                        _currentDirectory));
            }

            if (ScriptState == null)
            {
                ScriptState = await CSharpScript.RunAsync(
                                                    code,
                                                    ScriptOptions,
                                                    cancellationToken: cancellationToken)
                                                .UntilCancelled(cancellationToken);
            }
            else
            {
                ScriptState = await ScriptState.ContinueWithAsync(
                                                   code,
                                                   ScriptOptions,
                                                   catchException: catchException,
                                                   cancellationToken: cancellationToken)
                                               .UntilCancelled(cancellationToken);
            }

            if (ScriptState.Exception is null)
            {
                _workspace.UpdateWorkspace(ScriptState);
            }
        }

        public async Task HandleAsync(
            RequestCompletions command,
            KernelInvocationContext context)
        {
            var completionList =
                await GetCompletionList(
                    command.Code,
                    SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition));

            context.Publish(new CompletionsProduced(completionList, command));
        }

        private async Task<IEnumerable<CompletionItem>> GetCompletionList(
            string code,
            int cursorPosition)
        {

            using var _ = new GCPressure(1024 * 1024);

            var document = _workspace.UpdateWorkingDocument(code);
            var service = CompletionService.GetService(document);
            var completionList = await service.GetCompletionsAsync(document, cursorPosition);
           
            if (completionList is null)
            {
                return Enumerable.Empty<CompletionItem>();
            }

            var items = new List<CompletionItem>();
            foreach (var item in completionList.Items)
            {
                var description = await service.GetDescriptionAsync(document, item);
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
            var document = _workspace.UpdateWorkingDocument(command.Code);
            var semanticModel = await document.GetSemanticModelAsync();
            var diagnostics = semanticModel.GetDiagnostics();
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
        }

        public PackageRestoreContext PackageRestoreContext => _packageRestoreContext.Value;

        private bool HasReturnValue =>
            ScriptState != null &&
            (bool)_hasReturnValueMethod.Invoke(ScriptState.Script, null);

        void ISupportNuget.AddRestoreSource(string source) => _packageRestoreContext.Value.AddRestoreSource(source);

        PackageReference ISupportNuget.GetOrAddPackageReference(string packageName, string packageVersion) =>
            _packageRestoreContext.Value.GetOrAddPackageReference(
                packageName,
                packageVersion);

        void ISupportNuget.RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> resolvedReferences)
        {
            var references = resolvedReferences
                             .SelectMany(r => r.AssemblyPaths)
                             .Select(r => CachingMetadataResolver.ResolveReferenceWithXmlDocumentationProvider(r))
                             .ToArray();

            ScriptOptions = ScriptOptions.AddReferences(references);
        }

        Task<PackageRestoreResult> ISupportNuget.RestoreAsync() => _packageRestoreContext.Value.RestoreAsync();

        public IEnumerable<PackageReference> RequestedPackageReferences =>
            PackageRestoreContext.RequestedPackageReferences;

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences =>
            PackageRestoreContext.ResolvedPackageReferences;

        public IEnumerable<string> RestoreSources =>
            PackageRestoreContext.RestoreSources;
    }
}