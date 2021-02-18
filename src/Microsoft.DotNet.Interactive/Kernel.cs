// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive
{
    public abstract partial class Kernel : IDisposable
    {
        private readonly Subject<KernelEvent> _kernelEvents = new();
        private readonly CompositeDisposable _disposables;
        
        private readonly Dictionary<Type, KernelCommandInvocation> _dynamicHandlers = new();
        private FrontendEnvironment _frontendEnvironment;
        private ChooseKernelDirective _chooseKernelDirective;

        protected Kernel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;

            SubmissionParser = new SubmissionParser(this);

            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            Scheduler = new KernelCommandScheduler();

            AddSetKernelMiddleware();

            AddDirectiveMiddlewareAndCommonCommandHandlers();

            _disposables.Add(Disposable.Create(
                () => _kernelEvents.OnCompleted()
                ));
        }

        internal KernelCommandScheduler Scheduler { get; }

        internal KernelCommandPipeline Pipeline { get; }

        public CompositeKernel ParentKernel { get; internal set; }

        public SubmissionParser SubmissionParser { get; }

        public void AddMiddleware(
            KernelCommandPipelineMiddleware middleware,
            [CallerMemberName] string caller = null) => Pipeline.AddMiddleware(middleware, caller);

        public void DeferCommand(KernelCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.SetToken($"deferredCommand::{Guid.NewGuid():N}");
            Scheduler.DeferCommand(command, this);
        }

        private void AddSetKernelMiddleware()
        {
            AddMiddleware(SetKernel);
        }

        private void AddDirectiveMiddlewareAndCommonCommandHandlers()
        {
            AddMiddleware(
                async (originalCommand, context, next) =>
                {
                    var commands = PreprocessCommands(originalCommand);

                    if (!commands.Contains(originalCommand) && commands.Any())
                    {
                        context.CommandToSignalCompletion = commands.Last();
                    }

                    foreach (var command in commands)
                    {
                        if (context.IsComplete)
                        {
                            break;
                        }

                        if (command == originalCommand)
                        {
                            // no new context is needed
                            await next(originalCommand, context);
                        }
                        else
                        {
                            switch (command)
                            {
                                case AnonymousKernelCommand _:
                                case DirectiveCommand _:
                                    await command.InvokeAsync(context);
                                    break;
                                default:
                                    SetHandlingKernel(command, context);
                                    var kernel = context.HandlingKernel;
                                    if (kernel == this)
                                    {
                                        var c = KernelInvocationContext.Establish(command);
                                        await next(command, c);
                                    }
                                    else
                                    {
                                        // forward to appropriate kernel
                                        await kernel.SendAsync(command);
                                    }
                                    break;
                            }
                        }
                    }
                });
        }

        private IReadOnlyList<KernelCommand> PreprocessCommands(KernelCommand command)
        {
            return command switch
            {
                SubmitCode { LanguageNode: null } submitCode => SubmissionParser.SplitSubmission(submitCode),
                RequestDiagnostics { LanguageNode: null } requestDiagnostics => SubmissionParser.SplitSubmission(requestDiagnostics),
                LanguageServiceCommand { LanguageNode: null } languageServiceCommand => PreprocessLanguageServiceCommand(languageServiceCommand),
                _ => new[] { command }
            };
        }

        private IReadOnlyList<KernelCommand> PreprocessLanguageServiceCommand(LanguageServiceCommand command)
        {
            var commands = new List<KernelCommand>();
            var tree = SubmissionParser.Parse(command.Code, command.TargetKernelName);
            var rootNode = tree.GetRoot();
            var sourceText = SourceText.From(command.Code);

            // TextSpan.Contains only checks `[start, end)`, but we need to allow for `[start, end]`
            var absolutePosition = tree.GetAbsolutePosition(command.LinePosition);
            if (absolutePosition >= tree.Length)
            {
                absolutePosition--;
            }
            else if (char.IsWhiteSpace(rootNode.Text[absolutePosition]))
            {
                absolutePosition--;
            }

            if (rootNode.FindNode(absolutePosition) is LanguageNode node)
            {
                var nodeStartLine = sourceText.Lines.GetLinePosition(node.Span.Start).Line;
                var offsetNodeLine = command.LinePosition.Line - nodeStartLine;
                var position = new LinePosition(offsetNodeLine, command.LinePosition.Character);

                // create new command
                var offsetLanguageServiceCommand = command.With(
                    node,
                    position);

                offsetLanguageServiceCommand.TargetKernelName = node switch
                {
                    DirectiveNode _ => Name,
                    _ => node.KernelName,
                };

                commands.Add(offsetLanguageServiceCommand);
            }

            return commands;
        }

        private async Task SetKernel(KernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next)
        {
            SetHandlingKernel(command, context);

            await next(command, context);
        }

        public FrontendEnvironment FrontendEnvironment
        {
            get => _frontendEnvironment ??=
                       ParentKernel?.FrontendEnvironment ??
                       new AutomationEnvironment();
            set => _frontendEnvironment = value;
        }

        public IObservable<KernelEvent> KernelEvents => _kernelEvents;

        public string Name { get; set; }

        public IReadOnlyCollection<ICommand> Directives => SubmissionParser.Directives;

        public void AddDirective(Command command) => SubmissionParser.AddDirective(command);

        public void RegisterCommandHandler<TCommand>(Func<TCommand, KernelInvocationContext, Task> handler)
            where TCommand : KernelCommand
        {
            RegisterCommandType<TCommand>();
            _dynamicHandlers[typeof(TCommand)] = (command, context) => handler((TCommand)command, context);
        }

        public void RegisterCommandType<TCommand>()
            where TCommand : KernelCommand
        {
            KernelCommandEnvelope.RegisterCommandTypeReplacingIfNecessary<TCommand>();
        }

        internal virtual async Task HandleAsync(
            KernelCommand command,
            KernelInvocationContext context)
        {
            TrySetHandler(command, context);
            await command.InvokeAsync(context);
        }

        public Task<KernelCommandResult> SendAsync(
            KernelCommand command,
            CancellationToken cancellationToken)
        {
            return SendAsync(command, cancellationToken, null);
        }

        internal Task<KernelCommandResult> SendAsync(
            KernelCommand command,
            CancellationToken cancellationToken,
            Action onDone)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return Scheduler.Schedule(command, this, cancellationToken, onDone);

        }

        protected internal void CancelCommands()
        {
            Scheduler.CancelCommands();
        }

        internal Task RunDeferredCommandsAsync()
        {
            return Scheduler.RunDeferredCommandsAsync(this);

        }

        protected internal void PublishEvent(KernelEvent kernelEvent)
        {
            if (kernelEvent == null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            _kernelEvents.OnNext(kernelEvent);
        }

        public void RegisterForDisposal(Action dispose) => RegisterForDisposal(Disposable.Create(dispose));

        public void RegisterForDisposal(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            _disposables.Add(disposable);
        }

        private Task HandleRequestCompletionsAsync(
            RequestCompletions command,
            KernelInvocationContext context)
        {
            if (command.LanguageNode is DirectiveNode directiveNode)
            {
                var requestPosition = SourceText.From(command.Code)
                                                .Lines
                                                .GetPosition(command.LinePosition.ToCodeAnalysisLinePosition());

                var completions = GetDirectiveCompletionItems(
                    directiveNode,
                    requestPosition);

                var upToCursor =
                    directiveNode.Text[..command.LinePosition.Character];

                var indexOfPreviousSpace =
                    Math.Max(
                        0,
                        upToCursor.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase) + 1);

                var resultRange = new LinePositionSpan(
                    new LinePosition(command.LinePosition.Line, indexOfPreviousSpace),
                    command.LinePosition);

                context.Publish(
                    new CompletionsProduced(
                        completions, command, resultRange));
            }

            return Task.CompletedTask;
        }

        private IEnumerable<CompletionItem> GetDirectiveCompletionItems(
            DirectiveNode directiveNode,
            int requestPosition)
        {
            var directiveParsers = new List<Parser>();

            directiveParsers.AddRange(
                GetDirectiveParsersForCompletion(directiveNode, requestPosition));

            var allCompletions = new List<CompletionItem>();
            var topDirectiveParser = SubmissionParser.GetDirectiveParser();
            var prefix = topDirectiveParser.Configuration.RootCommand.Name + " ";
            requestPosition += prefix.Length;

            foreach (var parser in directiveParsers)
            {
                var effectiveText = $"{prefix}{directiveNode.Text}";

                var parseResult = parser.Parse(effectiveText);

                var suggestions = parseResult.GetSuggestions(requestPosition);

                var completions = suggestions
                                  .Select(s => SubmissionParser.CompletionItemFor(s, parseResult))
                                  .ToArray();

                allCompletions.AddRange(completions);
            }

            return allCompletions
                   .Distinct(CompletionItemComparer.Instance)
                   .ToArray();
        }

        private protected virtual IEnumerable<Parser> GetDirectiveParsersForCompletion(
            DirectiveNode directiveNode,
            int requestPosition)
        {
            yield return SubmissionParser.GetDirectiveParser();
        }

        private protected void TrySetHandler(
            KernelCommand command,
            KernelInvocationContext context)
        {
            if (command.Handler == null)
            {
                switch (command, this)
                {
                    case (ParseNotebook parseNotebook, IKernelCommandHandler<ParseNotebook> parseNotebookHandler):
                        SetHandler(parseNotebookHandler, parseNotebook);
                        break;

                    case (SerializeNotebook serializeNotebook, IKernelCommandHandler<SerializeNotebook> serializeNotebookHandler):
                        SetHandler(serializeNotebookHandler, serializeNotebook);
                        break;

                    case (SubmitCode submitCode, IKernelCommandHandler<SubmitCode> submitCodeHandler):
                        SetHandler(submitCodeHandler, submitCode);
                        break;

                    case (RequestCompletions rq, _)
                        when rq.LanguageNode is DirectiveNode:
                        rq.Handler = (__, ___) => HandleRequestCompletionsAsync(rq, context);
                        break;

                    case (RequestCompletions requestCompletion, IKernelCommandHandler<RequestCompletions> requestCompletionHandler):
                        SetHandler(requestCompletionHandler, requestCompletion);
                        break;

                    case (RequestDiagnostics requestDiagnostics, IKernelCommandHandler<RequestDiagnostics> requestDiagnosticsHandler):
                        SetHandler(requestDiagnosticsHandler, requestDiagnostics);
                        break;

                    case (RequestHoverText hoverCommand, IKernelCommandHandler<RequestHoverText> requestHoverTextHandler):
                        SetHandler(requestHoverTextHandler, hoverCommand);
                        break;

                    case (RequestSignatureHelp requestSignatureHelp, IKernelCommandHandler<RequestSignatureHelp> requestSignatureHelpHandler):
                        SetHandler(requestSignatureHelpHandler, requestSignatureHelp);
                        break;

                    default:
                        TrySetDynamicHandler(command, context);
                        break;
                }
            }
        }

        private void TrySetDynamicHandler(KernelCommand command, KernelInvocationContext context)
        {
            if (_dynamicHandlers.TryGetValue(command.GetType(), out KernelCommandInvocation handler))
            {
                command.Handler = handler;
            }
        }

        private static void SetHandler<T>(
            IKernelCommandHandler<T> handler,
            T command)
            where T : KernelCommand =>
            command.Handler = (_, context) =>
                handler.HandleAsync(command, context);

        protected virtual void SetHandlingKernel(
            KernelCommand command,
            KernelInvocationContext context) => context.HandlingKernel = this;

        public void Dispose() => _disposables.Dispose();

        protected virtual ChooseKernelDirective CreateChooseKernelDirective()
        {
            return new(this);
        }

        internal ChooseKernelDirective ChooseKernelDirective => _chooseKernelDirective ??= CreateChooseKernelDirective();
    }
}