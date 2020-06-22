// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public abstract class KernelBase : 
        IKernel
    {
        private readonly Subject<KernelEvent> _kernelEvents = new Subject<KernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly ConcurrentQueue<KernelCommand> _deferredCommands = new ConcurrentQueue<KernelCommand>();
        private readonly ConcurrentDictionary<Type, object> _properties = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentQueue<KernelOperation> _commandQueue =
            new ConcurrentQueue<KernelOperation>();
        private FrontendEnvironment _frontendEnvironment;

        protected KernelBase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;

            SubmissionParser = new SubmissionParser(this);

            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            AddCaptureConsoleMiddleware();

            AddSetKernelMiddleware();

            AddDirectiveMiddlewareAndCommonCommandHandlers();

            _disposables.Add(Disposable.Create( 
                ()  => _kernelEvents.OnCompleted()
                ));
        }

        internal KernelCommandPipeline Pipeline { get; }

        internal CompositeKernel ParentKernel { get; set; }

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

            _deferredCommands.Enqueue(command);
        }

        private void AddSetKernelMiddleware()
        {
            AddMiddleware(SetKernel);
        }

        private void AddCaptureConsoleMiddleware()
        {
            AddMiddleware(async (command, context, next) =>
            {
                using var console = await ConsoleOutput.TryCaptureAsync(c =>
                {
                    return new CompositeDisposable
                    {
                        c.Out.Subscribe(s => context.DisplayStandardOut(s, command)),
                        c.Error.Subscribe(s => context.DisplayStandardError(s, command))
                    };
                });

                await next(command, context);
            });
        }

        private void AddDirectiveMiddlewareAndCommonCommandHandlers()
        {
            AddMiddleware(
                async (originalCommand, context, next) =>
                {
                    var commands = PreprocessCommands(originalCommand, context);

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

        private IReadOnlyList<KernelCommand> PreprocessCommands(KernelCommand command, KernelInvocationContext context)
        {
            return command switch
            {
                SubmitCode submitCode
                when submitCode.LanguageNode is null => SubmissionParser.SplitSubmission(submitCode),

                LanguageServiceCommand languageServiceCommand
                when languageServiceCommand.LanguageNode is null => PreprocessLanguageServiceCommand(languageServiceCommand),

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
            var absolutePosition = tree.GetAbsolutePosition(command.Position);
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
                var offsetNodeLine = command.Position.Line - nodeStartLine;
                var position = new LinePosition(offsetNodeLine, command.Position.Character);

                // create new command
                var offsetLanguageServiceCommand = command.With(
                    node,
                    position);

                offsetLanguageServiceCommand.TargetKernelName = node switch
                {
                    DirectiveNode _ => Name,
                    _ => node.Language,
                };

                commands.Add(offsetLanguageServiceCommand);
            }

            return commands;
        }

        private async Task SetKernel(KernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next)
        {
            SetHandlingKernel(command, context);

            var previousKernel = context.CurrentKernel;

            context.CurrentKernel = this;

            await next(command, context);

            context.CurrentKernel = previousKernel;
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
        
        private class KernelOperation
        {
            public KernelOperation(KernelCommand command, TaskCompletionSource<IKernelCommandResult> taskCompletionSource)
            {
                Command = command;
                TaskCompletionSource = taskCompletionSource;
            }

            public KernelCommand Command { get; }

            public TaskCompletionSource<IKernelCommandResult> TaskCompletionSource { get; }
        }

        private async Task ExecuteCommand(KernelOperation operation)
        {
            var context = KernelInvocationContext.Establish(operation.Command);

            // only subscribe for the root command 
            using var _ =
                context.Command == operation.Command
                ? context.KernelEvents.Subscribe(PublishEvent)
                : Disposable.Empty;

            try
            {
                await Pipeline.SendAsync(operation.Command, context);

                if (operation.Command == context.Command)
                {
                    await context.DisposeAsync();
                }
                else
                {
                    context.Complete(operation.Command);
                }

                operation.TaskCompletionSource.SetResult(context.Result);
            }
            catch (Exception exception)
            {
                if (!context.IsComplete)
                {
                    context.Fail(exception);
                }

                operation.TaskCompletionSource.SetException(exception);
            }
        }

        internal virtual async Task HandleAsync(
            KernelCommand command,
            KernelInvocationContext context)
        {
            TrySetHandler(command, context);
            await command.InvokeAsync(context);
        }

        public Task<IKernelCommandResult> SendAsync(
            KernelCommand command,
            CancellationToken cancellationToken)
        {
            return SendAsync(command, cancellationToken, null);
        }

        internal Task<IKernelCommandResult> SendAsync(
            KernelCommand command,
            CancellationToken cancellationToken, 
            Action onDone)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            UndeferCommands();

            var tcs = new TaskCompletionSource<IKernelCommandResult>();

            var operation = new KernelOperation(command, tcs);
           
            _commandQueue.Enqueue(operation);

            ProcessCommandQueue(_commandQueue, cancellationToken, onDone);

            return tcs.Task;
        }

        private void ProcessCommandQueue(
            ConcurrentQueue<KernelOperation> commandQueue,
            CancellationToken cancellationToken,
            Action onDone)
        {
            if (commandQueue.TryDequeue(out var currentOperation))
            {
                Task.Run(async () =>
                {
                    await ExecuteCommand(currentOperation);

                    ProcessCommandQueue(commandQueue, cancellationToken,onDone);
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                onDone?.Invoke();
            }
        }

        internal Task RunDeferredCommandsAsync()
        {
            var tcs = new TaskCompletionSource<Unit>();
            UndeferCommands();
            ProcessCommandQueue(
                _commandQueue, 
                CancellationToken.None,
                () => tcs.SetResult(Unit.Default));
            return tcs.Task;
        }

        private void UndeferCommands()
        {
            while (_deferredCommands.TryDequeue(out var initCommand))
            {
                _commandQueue.Enqueue(new KernelOperation(initCommand, new TaskCompletionSource<IKernelCommandResult>()));
            }
        }

        protected void PublishEvent(KernelEvent kernelEvent)
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

        private Task HandleRequestCompletionAsync(
            RequestCompletion command,
            KernelInvocationContext context)
        {
            if (command.LanguageNode is DirectiveNode directiveNode)
            {
                var requestPosition = SourceText.From(command.Code)
                                                .Lines
                                                .GetPosition(command.Position);

                var resultRange = new LinePositionSpan(
                    new LinePosition(command.Position.Line, 0),
                    command.Position);

                var completions = GetDirectiveCompletionItems(directiveNode, requestPosition);

                context.Publish(
                    new CompletionRequestCompleted(
                        completions, command, resultRange));
            }

            return Task.CompletedTask;
        }

        private protected virtual IReadOnlyList<CompletionItem> GetDirectiveCompletionItems(
            DirectiveNode directiveNode, 
            int requestPosition)
        {
            var parseResult = directiveNode.GetDirectiveParseResult();

            var completions = parseResult
                              .GetSuggestions(requestPosition)
                              .Select(s => SubmissionParser.CompletionItemFor(s, parseResult))
                              .ToArray();

            return completions;
        }

        private protected void TrySetHandler(
            KernelCommand command,
            KernelInvocationContext context)
        {
            if (command is KernelCommand kb)
            {
                if (kb.Handler == null)
                {
                    switch (command, this)
                    {
                        case (SubmitCode submitCode, IKernelCommandHandler<SubmitCode> submitCodeHandler):
                            SetHandler(submitCodeHandler, submitCode);
                            break;

                        case (RequestCompletion rq, _)
                            when rq.LanguageNode is DirectiveNode:
                            rq.Handler = (__, ___) => HandleRequestCompletionAsync(rq, context);
                            break;

                        case (RequestCompletion requestCompletion, IKernelCommandHandler<RequestCompletion> requestCompletionHandler):
                            SetHandler(requestCompletionHandler, requestCompletion);
                            break;

                        case (RequestHoverText hoverCommand, IKernelCommandHandler<RequestHoverText> requestHoverTextHandler):
                            SetHandler(requestHoverTextHandler, hoverCommand);
                            break;

                        case (ChangeWorkingDirectory cwd, _):
                            cwd.Handler = (__, ___) =>
                            {
                                // FIX: (TrySetHandler) move this to the command class
                                Directory.SetCurrentDirectory(cwd.WorkingDirectory.FullName);
                                context.Publish(new WorkingDirectoryChanged(cwd.WorkingDirectory, cwd));
                                return Task.CompletedTask;
                            };
                            break;
                    }
                }
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

        string IKernel.Name => Name;

        public void SetProperty<T>(T property) where T : class
        {
            if (!_properties.TryAdd(typeof(T), property))
            {
                throw new InvalidOperationException($"Cannot add property with key {typeof(T)}.");
            }
        }

        public T GetProperty<T>() where T : class
        {
            return _properties.TryGetValue(typeof(T), out var property) 
                ? property as T 
                : throw new KeyNotFoundException($"Cannot find property {typeof(T)}.");
        }
    }
}