// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.LanguageService;
using Microsoft.DotNet.Interactive.Utility;
using XPlot.Plotly;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public class CSharpKernel :
        DotNetLanguageKernel,
        IExtensibleKernel,
        ISupportNuget,
        IKernelCommandHandler<RequestCompletion>,
        IKernelCommandHandler<RequestHoverText>,
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
                             typeof(IKernel).Assembly,
                             typeof(CSharpKernel).Assembly,
                             typeof(PocketView).Assembly,
                             typeof(PlotlyChart).Assembly);

        private readonly AssemblyBasedExtensionLoader _extensionLoader = new AssemblyBasedExtensionLoader();
        private string _currentDirectory;

        public CSharpKernel() : base(DefaultKernelName)
        {
            _workspace = new InteractiveWorkspace();

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

        public override bool TryGetVariable<T>(
            string name,
            out T value)
        {
            if (ScriptState?.Variables
                           .LastOrDefault(v => v.Name == name) is { } variable)
            {
                value = (T) variable.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override async Task SetVariableAsync(string name, object value)
        {
            var csharpTypeDeclaration = new StringWriter();

            value.GetType().WriteCSharpDeclarationTo(csharpTypeDeclaration);

            await RunAsync($"{csharpTypeDeclaration} {name} = default;");

            var scriptVariable = ScriptState.GetVariable(name);

            scriptVariable.Value = value;
        }

        public async Task HandleAsync(RequestHoverText command, KernelInvocationContext context)
        {
            var document = _workspace.ForkDocument(command.Code);
            var text = await document.GetTextAsync();
            var cursorPosition = text.Lines.GetPosition(command.Position);
            var service = QuickInfoService.GetService(document);
            var info = await service.GetQuickInfoAsync(document, cursorPosition);

            if (info == null)
            {
                return;
            }

            var scriptSpanStart = text.Lines.GetLinePosition(0);
            var linePosSpan = text.Lines.GetLinePositionSpan(info.Span);
            var correctedLinePosSpan = linePosSpan.SubtractLineOffset(scriptSpanStart);

            context.PublishHoverTextMarkdownResponse(command, info.ToMarkdownString(), correctedLinePosSpan);
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
                if (exception != null)
                {
                    string message = null;

                    if (exception is CodeSubmissionCompilationErrorException compilationError)
                    {
                        message =
                            string.Join(Environment.NewLine,
                                        (compilationError.InnerException as CompilationErrorException)?.Diagnostics.Select(d => d.ToString()) ?? Enumerable.Empty<string>());
                    }

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
                    ScriptMetadataResolver.Default.WithBaseDirectory(
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
                await _workspace.AddSubmissionAsync(ScriptState);
            }
        }

        public async Task HandleAsync(
            RequestCompletion requestCompletion,
            KernelInvocationContext context)
        {
            var completionRequestReceived = new CompletionRequestReceived(requestCompletion);

            context.Publish(completionRequestReceived);

            var completionList =
                await GetCompletionList(
                    requestCompletion.Code,
                    SourceUtilities.GetCursorOffsetFromPosition(requestCompletion.Code, requestCompletion.Position));

            context.Publish(new CompletionRequestCompleted(completionList, requestCompletion));
        }

        private async Task<IEnumerable<CompletionItem>> GetCompletionList(
            string code,
            int cursorPosition)
        {
            var document = _workspace.ForkDocument(code);

            var service = CompletionService.GetService(document);
            
            var completionList = await service.GetCompletionsAsync(document, cursorPosition);

            if (completionList is null)
            {
                return Enumerable.Empty<CompletionItem>();
            }

            var semanticModel = await document.GetSemanticModelAsync();
            var symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(
                              semanticModel, 
                              cursorPosition, 
                              document.Project.Solution.Workspace);

            var symbolToSymbolKey = new Dictionary<(string, int), ISymbol>();
            foreach (var symbol in symbols)
            {
                var key = (symbol.Name, (int) symbol.Kind);
                if (!symbolToSymbolKey.ContainsKey(key))
                {
                    symbolToSymbolKey[key] = symbol;
                }
            }

            var items = completionList.Items.Select(item => item.ToModel(symbolToSymbolKey, document)).ToArray();

            return items;
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
            (bool) _hasReturnValueMethod.Invoke(ScriptState.Script, null);

        void ISupportNuget.AddRestoreSource(string source) => _packageRestoreContext.Value.AddRestoreSource(source);

        PackageReference ISupportNuget.GetOrAddPackageReference(string packageName, string packageVersion) =>
            _packageRestoreContext.Value.GetOrAddPackageReference(
                packageName,
                packageVersion);

        void ISupportNuget.RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> resolvedReferences)
        {
            var references = resolvedReferences
                             .SelectMany(r => r.AssemblyPaths)
                             .Select(r => MetadataReference.CreateFromFile(r.FullName));

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