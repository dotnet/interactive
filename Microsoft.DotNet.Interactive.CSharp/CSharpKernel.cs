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
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using XPlot.Plotly;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public class CSharpKernel : 
        KernelBase, 
        IExtensibleKernel
    {
        internal const string DefaultKernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        protected CSharpParseOptions _csharpParseOptions =
            new CSharpParseOptions(LanguageVersion.Default, kind: SourceCodeKind.Script);

        private WorkspaceFixture _fixture;
        private CancellationTokenSource _cancellationSource;
        private readonly object _cancellationSourceLock = new object();

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

        public CSharpKernel()
        {
            _cancellationSource = new CancellationTokenSource();
            Name = DefaultKernelName;
            NativeAssemblyLoadHelper = new NativeAssemblyLoadHelper(this);
            RegisterForDisposal(NativeAssemblyLoadHelper);
            RegisterForDisposal(() =>
            {
                ScriptState = null;
            });
        }

        public ScriptState ScriptState { get; private set; }

        internal void AddScriptReferences(IReadOnlyList<ResolvedPackageReference> assemblyPaths)
        {
            var references = assemblyPaths
                             .SelectMany(r => r.AssemblyPaths)
                             .Select(r => MetadataReference.CreateFromFile(r.FullName));

            ScriptOptions = ScriptOptions.AddReferences(references);
        }

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

        public Task<bool> IsCompleteSubmissionAsync(string code)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, _csharpParseOptions);
            return Task.FromResult(SyntaxFactory.IsCompleteSubmission(syntaxTree));
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
            using var console = await ConsoleOutput.CaptureAsync();
            using (console.SubscribeToStandardOutput(std => context.PublishStandardOut(std, submitCode)))
            using (console.SubscribeToStandardError(std => context.PublishStandardError(std, submitCode)))
            {
                if (!cancellationSource.IsCancellationRequested)
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
                                    cancellationToken: cancellationSource.Token)
                                .UntilCancelled(cancellationSource.Token);
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
                                    cancellationToken: cancellationSource.Token)
                                .UntilCancelled(cancellationSource.Token);
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
            }

            if (!cancellationSource.IsCancellationRequested)
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

        private bool HasReturnValue =>
            ScriptState != null &&
            (bool)_hasReturnValueMethod.Invoke(ScriptState.Script, null);

        internal NativeAssemblyLoadHelper NativeAssemblyLoadHelper { get; }
    }
}