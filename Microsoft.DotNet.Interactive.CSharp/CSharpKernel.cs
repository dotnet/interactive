// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Interactive.DependencyManager;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.LanguageService;
using Microsoft.DotNet.Interactive.Utility;
using Newtonsoft.Json.Linq;
using XPlot.Plotly;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public class CSharpKernel :
        KernelBase,
        IExtensibleKernel,
        ISupportNuget
    {
        internal const string DefaultKernelName = "csharp";

        private object lockObject = new object();
        private DependencyProvider _dependencies;

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        protected CSharpParseOptions _csharpParseOptions =
            new CSharpParseOptions(LanguageVersion.Default, kind: SourceCodeKind.Script);

        private WorkspaceFixture _fixture;

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
            RegisterForDisposal(() =>
            {
                ScriptState = null;
                (_dependencies as IDisposable)?.Dispose();
            });
        }

        public ScriptState ScriptState { get; private set; }

        public Task<bool> IsCompleteSubmissionAsync(string code)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, _csharpParseOptions);
            return Task.FromResult(SyntaxFactory.IsCompleteSubmission(syntaxTree));
        }

        public override bool TryGetVariable(
            string name,
            out object value)
        {
            if (ScriptState?.Variables
                           .LastOrDefault(v => v.Name == name) is { } variable)
            {
                value = variable.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override Task<LspResponse> LspMethod(string methodName, JObject request)
        {
            LspResponse result;
            switch (methodName)
            {
                case "textDocument/hover":
                    // https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
                    var hoverParams = request.ToLspObject<HoverParams>();
                    result = TextDocumentHover(hoverParams);
                    break;
                default:
                    result = null;
                    break;
            }

            return Task.FromResult(result);
        }

        public TextDocumentHoverResponse TextDocumentHover(HoverParams hoverParams)
        {
            return new TextDocumentHoverResponse()
            {
                Contents = new MarkupContent()
                {
                    Kind = MarkupKind.Markdown,
                    Value = $"textDocument/hover at position ({hoverParams.Position.Line}, {hoverParams.Position.Character}) with `markdown`",
                },
            };
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
                ScriptOptions = ScriptOptions.WithMetadataResolver(
                    ScriptMetadataResolver.Default.WithBaseDirectory(
                        Directory.GetCurrentDirectory()));

                try
                {
                    if (ScriptState == null)
                    {
                        ScriptState = await CSharpScript.RunAsync(
                                                            code,
                                                            ScriptOptions,
                                                            cancellationToken: context.CancellationToken)
                                                        .UntilCancelled(context.CancellationToken);
                    }
                    else
                    {
                        ScriptState = await ScriptState.ContinueWithAsync(
                                                           code,
                                                           ScriptOptions,
                                                           catchException: e =>
                                                           {
                                                               exception = e;
                                                               return true;
                                                           },
                                                           cancellationToken: context.CancellationToken)
                                                       .UntilCancelled(context.CancellationToken);
                    }
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
            var compilation = ScriptState.Script.GetCompilation();
            var originalCode =
                ScriptState?.Script.Code ?? string.Empty;

            var buffer = new StringBuilder(originalCode);
            if (!string.IsNullOrWhiteSpace(originalCode) && !originalCode.EndsWith(Environment.NewLine))
            {
                buffer.AppendLine();
            }

            buffer.AppendLine(code);
            var fullScriptCode = buffer.ToString();
            var offset = fullScriptCode.LastIndexOf(code, StringComparison.InvariantCulture);
            var absolutePosition = Math.Max(offset, 0) + cursorPosition;

            if (_fixture == null || ShouldRebuild())
            {
                _fixture = new WorkspaceFixture(compilation.Options, compilation.References);
            }

            var document = _fixture.ForkDocument(fullScriptCode);
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

            bool ShouldRebuild()
            {
                return compilation.References.Count() != _fixture.MetadataReferences.Count();
            }
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

        //private Lazy<DependencyProvider> _dependencies2 = new Lazy<DependencyProvider>(() => GetDependencyProvider());

        //private DependencyProvider GetDependencyProvider()
        //{
        //    var iSupportNuget = this as ISupportNuget;
        //    return new DependencyProvider(iSupportNuget.AssemblyProbingPaths, iSupportNuget.NativeProbingRoots);
        //}

        AssemblyResolutionProbe ISupportNuget.AssemblyProbingPaths { get; set; }

        NativeResolutionProbe ISupportNuget.NativeProbingRoots { get; set; }

        void ISupportNuget.RegisterNugetResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> resolvedReferences)
        {
            var references = resolvedReferences
                             .SelectMany(r => r.AssemblyPaths)
                             .Select(r => MetadataReference.CreateFromFile(r.FullName));

            ScriptOptions = ScriptOptions.AddReferences(references);
        }

        IResolveDependenciesResult ISupportNuget.Resolve(IEnumerable<string> packageManagerTextLines, string executionTfm, ResolvingErrorReport reportError)
        {
            // C# does not allow a static field to have a func that references an instance field.
            //
            if (_dependencies == null)
            {
                lock (lockObject)
                {
                    if (_dependencies == null)
                    {
                        var iSupportNuget = this as ISupportNuget;
                        _dependencies = new DependencyProvider(iSupportNuget.AssemblyProbingPaths, iSupportNuget.NativeProbingRoots);
                    }
                }
            }

            IDependencyManagerProvider iDependencyManager = _dependencies.TryFindDependencyManagerByKey(Enumerable.Empty<string>(), "", reportError, "nuget");
            if (iDependencyManager == null)
            {
                // If this happens it is because of a bug in the Dependency provider. or deployment failed to deploy the nuget provider dll.
                // We guarantee the presence of the nuget provider, by shipping it with the notebook product
                throw new InvalidOperationException("Internal error - must invoke ISupportNuget.InitializeDependencyProvider before ISupportNuget.Resolve()");
            }

            return _dependencies.Resolve(iDependencyManager, ".csx", packageManagerTextLines, reportError, executionTfm);
        }

        private bool HasReturnValue =>
            ScriptState != null &&
            (bool)_hasReturnValueMethod.Invoke(ScriptState.Script, null);
    }
}