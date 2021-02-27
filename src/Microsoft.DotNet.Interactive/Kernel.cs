// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        private readonly KernelCommandScheduler _scheduler;
        private KernelScheduler<KernelCommand, KernelCommandResult> _commandScheduler;

        private readonly ConcurrentQueue<KernelCommand> _deferredCommands = new();

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

            _scheduler = new KernelCommandScheduler();

            _disposables.Add(Disposable.Create(
                () => _kernelEvents.OnCompleted()
                ));
        }
        
        internal KernelCommandScheduler Scheduler => _scheduler;

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
          
            
            NewDeferCommand(command);
        }

        private void NewDeferCommand(KernelCommand command)
        {
           _deferredCommands.Enqueue(command);
        }

        private bool TryPreprocessCommands(
            KernelCommand originalCommand,
            KernelInvocationContext context, 
            out IReadOnlyList<KernelCommand> commands)
        {
            switch (originalCommand)
            {
                case SubmitCode {LanguageNode: null} submitCode:
                    commands = SubmissionParser.SplitSubmission(submitCode);
                    break;
                case RequestDiagnostics {LanguageNode: null} requestDiagnostics:
                    commands = SubmissionParser.SplitSubmission(requestDiagnostics);
                    break;
                case LanguageServiceCommand {LanguageNode: null} languageServiceCommand:
                    if (!TryPreprocessLanguageServiceCommand(languageServiceCommand, context, out commands))
                    {
                        return false;
                    }
                    break;
                default:
                    commands = new[] {originalCommand};
                    break;
            }

            foreach (var command in commands)
            {
                if (command.KernelUri is null)
                {
                    command.KernelUri = GetHandlingKernelUri(command);
                }

                if (command.Parent is null && 
                    command != originalCommand)
                {
                    command.Parent = originalCommand;
                }
            }
            
            return true;
        }

        private bool TryPreprocessLanguageServiceCommand(LanguageServiceCommand command, KernelInvocationContext context, out IReadOnlyList<KernelCommand> commands)
        {
            var postProcessCommands = new List<KernelCommand>();
            var tree = SubmissionParser.Parse(command.Code, command.TargetKernelName);
            var rootNode = tree.GetRoot();
            var sourceText = SourceText.From(command.Code);
            var lines = sourceText.Lines;
            if(command.LinePosition.Line < 0 
               || command.LinePosition.Line >= lines.Count
               || command.LinePosition.Character < 0
               || command.LinePosition.Character > lines[command.LinePosition.Line].Span.Length)
            {
                context.Fail(message:$"The specified position {command.LinePosition}");
                commands = null;
                return false;
            }
            
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
                    DirectiveNode => Name,
                    _ => node.KernelName,
                };

                postProcessCommands.Add(offsetLanguageServiceCommand);
            }

            commands = postProcessCommands;

            return true;
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
        
        internal KernelUri Uri =>
            ParentKernel is null
                ? KernelUri.Parse(Name)
                : ParentKernel.Uri.Append($"{Name}");

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

        public async Task<KernelCommandResult> SendAsync(
            KernelCommand command,
            CancellationToken cancellationToken)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var scheduler = GetOrCreateScheduler();
            var context = KernelInvocationContext.Establish(command);

            // only subscribe for the root command 
            var currentCommandOwnsContext = context.Command == command;

            using var disposable = currentCommandOwnsContext
                                       ? context.KernelEvents.Subscribe(PublishEvent)
                                       : Disposable.Empty;

            if (TryPreprocessCommands(command, context, out var commands))
            {
                foreach (var command1 in commands)
                {
                    await scheduler.ScheduleAndWaitForCompletionAsync(command1, InvokePipelineAndCommandHandler, command1.KernelUri.ToString());
                }

                if (currentCommandOwnsContext)
                {
                    await context.DisposeAsync();
                }
            }

            return context.Result;
        }

        protected KernelScheduler<KernelCommand, KernelCommandResult> GetOrCreateScheduler()
        {
            if (_commandScheduler is null)
            {
                SetScheduler(new KernelScheduler<KernelCommand, KernelCommandResult>());
            }

            return _commandScheduler;
        }

        internal void SetScheduler(KernelScheduler<KernelCommand, KernelCommandResult> scheduler)
        {
            _commandScheduler = scheduler;

            IEnumerable<KernelCommand> GetDeferredOperations(KernelCommand command, string scope)
            {
                if (!command.KernelUri.Contains(Uri))
                {
                    yield break;
                }

                while (_deferredCommands.TryDequeue(out var kernelCommand))
                {
                    var currentInvocationContext = KernelInvocationContext.Current;
                    if (TryPreprocessCommands(kernelCommand, currentInvocationContext, out var commands))
                    {
                        foreach (var cmd in commands)
                        {
                            yield return cmd;
                        }
                    }
                }
            }

            _commandScheduler.RegisterDeferredOperationSource(GetDeferredOperations, InvokePipelineAndCommandHandler);
        }

        private protected virtual KernelUri GetHandlingKernelUri(
            KernelCommand command)
        {
            return Uri;
        }

        internal async Task<KernelCommandResult> InvokePipelineAndCommandHandler(KernelCommand command)
        {
            var context = KernelInvocationContext.Establish(command);

            try
            {
                SetHandlingKernel(command,context);

                await Pipeline.SendAsync(command, context);

                if (command != context.Command)
                {
                    context.Complete(command);
                }

                return context.Result;
            }
            catch (Exception exception)
            {
                if (!context.IsComplete)
                {
                    context.Fail(exception);
                }

                throw;
            }
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
                        TrySetDynamicHandler(command);
                        break;
                }
            }
        }

        private void TrySetDynamicHandler(KernelCommand command)
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