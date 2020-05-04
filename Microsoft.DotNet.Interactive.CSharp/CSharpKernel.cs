// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.LanguageService;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.Interactive.DependencyManager;
using XPlot.Plotly;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public class CSharpKernel :
        DotNetLanguageKernel,
        IExtensibleKernel,
        ISupportNuget,
        IKernelCommandHandler<RequestHoverText>
    {
        internal const string DefaultKernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        protected CSharpParseOptions _csharpParseOptions =
            new CSharpParseOptions(LanguageVersion.Default, kind: SourceCodeKind.Script);

        private WorkspaceFixture _fixture;
        private AssemblyResolutionProbe _assemblyProbingPaths;
        private readonly Lazy<DependencyProvider> _dependencies;
        private NativeResolutionProbe _nativeProbingRoots;

        internal ScriptOptions ScriptOptions =
            ScriptOptions.Default
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

        public CSharpKernel() : base(DefaultKernelName)
        {
            _dependencies = new Lazy<DependencyProvider>(GetDependencyProvider);

            RegisterForDisposal(() =>
            {
                ScriptState = null;
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
            if (!command.DocumentIdentifier.TryDecodeDocumentFromDataUri(out var documentContents))
            {
                return;
            }

            var (document, offset) = GetDocumentWithOffsetFromCode(documentContents);
            var text = await document.GetTextAsync();
            var cursorPosition = text.Lines.GetPosition(new LinePosition(command.Position.Line, command.Position.Character));
            var absolutePosition = cursorPosition + offset;
            var service = QuickInfoService.GetService(document);
            var info = await service.GetQuickInfoAsync(document, absolutePosition);
            if (info == null)
            {
                return;
            }

            var scriptSpanStart = text.Lines.GetLinePosition(offset);
            var linePosSpan = text.Lines.GetLinePositionSpan(info.Span);
            var correctedLinePosSpan = linePosSpan.SubtractLineOffset(scriptSpanStart);

            context.PublishHoverTextMarkdownResponse(command, info.ToMarkdownString(), correctedLinePosSpan);
        }

        protected override async Task HandleSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext context)
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
            ScriptOptions = ScriptOptions.WithMetadataResolver(
                ScriptMetadataResolver.Default.WithBaseDirectory(
                    Directory.GetCurrentDirectory()));

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
        }

        protected override async Task HandleRequestCompletion(
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

        private async Task<IEnumerable<CompletionItem>> GetCompletionList(
            string code,
            int cursorPosition)
        {
            var (document, offset) = GetDocumentWithOffsetFromCode(code);
            var text = await document.GetTextAsync();
            var absolutePosition = cursorPosition + offset;

            var service = CompletionService.GetService(document);

            var completionList = await service.GetCompletionsAsync(document, absolutePosition);
            var semanticModel = await document.GetSemanticModelAsync();
            var symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(semanticModel, absolutePosition, document.Project.Solution.Workspace);

            var symbolToSymbolKey = new Dictionary<(string, int), ISymbol>();
            foreach (var symbol in symbols)
            {
                var key = (symbol.Name, (int)symbol.Kind);
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

        private DependencyProvider GetDependencyProvider()
        {
            // These may not be set to null, if they are it is a product coding error
            // ISupportNuget.Initialize must be invoked prior to creating the DependencyManager
            if (_assemblyProbingPaths == null)
            {
                throw new ArgumentNullException(nameof(_assemblyProbingPaths));
            }
            if (_nativeProbingRoots == null)
            {
                throw new ArgumentNullException(nameof(_nativeProbingRoots));
            }

            var dependencyProvider = new DependencyProvider(
                _assemblyProbingPaths, 
                _nativeProbingRoots);

            RegisterForDisposal(dependencyProvider);

            return dependencyProvider;
        }

        // Set assemblyProbingPaths, nativeProbingRoots for Kernel.
        // These values are functions that return the list of discovered assemblies, and package roots
        // They are used by the dependecymanager for Assembly and Native dll resolving
        void ISupportNuget.Initialize(AssemblyResolutionProbe assemblyProbingPaths, NativeResolutionProbe nativeProbingRoots)
        {
            _assemblyProbingPaths = assemblyProbingPaths ?? throw new ArgumentNullException(nameof(assemblyProbingPaths));
            _nativeProbingRoots = nativeProbingRoots ?? throw new ArgumentNullException(nameof(nativeProbingRoots));
        }

        void ISupportNuget.RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> resolvedReferences)
        {
            var references = resolvedReferences
                             .SelectMany(r => r.AssemblyPaths)
                             .Select(r => MetadataReference.CreateFromFile(r.FullName));

            ScriptOptions = ScriptOptions.AddReferences(references);
        }

        IResolveDependenciesResult ISupportNuget.Resolve(IEnumerable<string> packageManagerTextLines, string executionTfm, ResolvingErrorReport reportError)
        {
            IDependencyManagerProvider iDependencyManager = _dependencies.Value.TryFindDependencyManagerByKey(Enumerable.Empty<string>(), "", reportError, "nuget");
            if (iDependencyManager == null)
            {
                // If this happens it is because of a bug in the Dependency provider. or deployment failed to deploy the nuget provider dll.
                // We guarantee the presence of the nuget provider, by shipping it with the notebook product
                throw new InvalidOperationException("Internal error - unable to locate the nuget package manager, please try to reinstall.");
            }

            return _dependencies.Value.Resolve(iDependencyManager, ".csx", packageManagerTextLines, reportError, executionTfm);
        }

        private (Document document, int offset) GetDocumentWithOffsetFromCode(string code)
        {
            var compilation = ScriptState.Script.GetCompilation();
            var originalCode = ScriptState.Script.Code ?? string.Empty;

            var buffer = new StringBuilder(originalCode);
            if (!string.IsNullOrWhiteSpace(originalCode) && !originalCode.EndsWith(Environment.NewLine))
            {
                buffer.AppendLine();
            }

            var offset = buffer.Length;
            buffer.AppendLine(code);
            var fullScriptCode = buffer.ToString();

            if (_fixture == null || ShouldRebuild())
            {
                _fixture = new WorkspaceFixture(compilation.Options, compilation.References);
            }

            var document = _fixture.ForkDocument(fullScriptCode);
            return (document, offset);

            bool ShouldRebuild()
            {
                return compilation.References.Count() != _fixture.MetadataReferences.Count();
            }
        }

        private bool HasReturnValue =>
            ScriptState != null &&
            (bool)_hasReturnValueMethod.Invoke(ScriptState.Script, null);
    }
}