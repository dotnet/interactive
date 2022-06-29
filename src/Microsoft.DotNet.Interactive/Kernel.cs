// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.ValueSharing;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Kernel>;
using Pocket;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive
{
    public abstract partial class Kernel : 
        IKernelCommandHandler<RequestKernelInfo>, 
        IKernelCommandHandler<RequestValue>, 
        IKernelCommandHandler<RequestValueInfos>, 
        IDisposable
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<Type>> _declaredHandledCommandTypesByKernelType = new();
        private readonly HashSet<Type> _supportedCommandTypes;

        private readonly Subject<KernelEvent> _kernelEvents = new();
        private readonly CompositeDisposable _disposables;
        private readonly ConcurrentDictionary<Type, KernelCommandInvocation> _dynamicHandlers = new();
        private readonly ImmediateScheduler<KernelCommand, KernelCommandResult> _fastPathScheduler = new();
        private FrontendEnvironment _frontendEnvironment;
        private ChooseKernelDirective _chooseKernelDirective;
        private KernelScheduler<KernelCommand, KernelCommandResult> _commandScheduler;
        private readonly ConcurrentQueue<KernelCommand> _deferredCommands = new();
        private KernelInvocationContext _inFlightContext;
        private int _countOfLanguageServiceCommandsInFlight = 0;
        private readonly KernelInfo _kernelInfo;

        protected Kernel(
            string name,
            string languageName = null,
            string languageVersion = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            RootKernel = this;

            Name = name;

            SubmissionParser = new SubmissionParser(this);

            _disposables = new CompositeDisposable();
            _disposables.Add(Disposable.Create(() => _kernelEvents.OnCompleted()));

            Pipeline = new KernelCommandPipeline(this);

            _supportedCommandTypes = new HashSet<Type>(
                _declaredHandledCommandTypesByKernelType
                    .GetOrAdd(
                        GetType(),
                        InitializeSupportedCommandTypes));

            var supportedKernelCommands = _supportedCommandTypes.Select(t => new KernelCommandInfo(t.Name)).ToArray();

            var supportedDirectives = Directives.Select(d => new KernelDirectiveInfo(d.Name)).ToArray();

            _kernelInfo = new KernelInfo(name, languageName, languageVersion)
            {
                SupportedKernelCommands = supportedKernelCommands,
                SupportedDirectives = supportedDirectives,
            };

            IReadOnlyCollection<Type> InitializeSupportedCommandTypes(Type kernelType)
            {
                return kernelType.GetInterfaces()
                                 .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKernelCommandHandler<>))
                                 .SelectMany(i => i.GenericTypeArguments)
                                 .ToArray();
            }
        }

        internal KernelCommandPipeline Pipeline { get; }

        public CompositeKernel ParentKernel { get; internal set; }

        public Kernel RootKernel { get; internal set; }

        public SubmissionParser SubmissionParser { get; }

        public void AddMiddleware(
            KernelCommandPipelineMiddleware middleware,
            [CallerMemberName] string caller = null) => Pipeline.AddMiddleware(middleware, caller);

        public void DeferCommand(KernelCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.SetToken($"deferredCommand::{Guid.NewGuid():N}");

            _deferredCommands.Enqueue(command);
        }

        private bool TryPreprocessCommands(
            KernelCommand originalCommand,
            KernelInvocationContext context,
            out IReadOnlyList<KernelCommand> commands)
        {
            switch (originalCommand)
            {
                case SubmitCode { LanguageNode: null } submitCode:
                    commands = SubmissionParser.SplitSubmission(submitCode);
                    break;

                case RequestDiagnostics { LanguageNode: null } requestDiagnostics:
                    commands = SubmissionParser.SplitSubmission(requestDiagnostics);
                    break;

                case LanguageServiceCommand { LanguageNode: null } languageServiceCommand:
                    if (!TryAdjustLanguageServiceCommandLinePositions(languageServiceCommand, context, out var adjustedCommand))
                    {
                        commands = null;
                        return false;
                    }

                    commands = new[] { adjustedCommand };
                    break;

                default:
                    commands = new[] { originalCommand };
                    break;
            }

            foreach (var command in commands)
            {
                var handlingKernel = GetHandlingKernel(command, context);

                if (handlingKernel is null)
                {
                    context.Fail(command, new NoSuitableKernelException(command));
                    return false;
                }

                command.SchedulingScope ??= handlingKernel.SchedulingScope;
                command.TargetKernelName ??= handlingKernel.Name;

                if (command.Parent is null && 
                    !CommandEqualityComparer.Instance.Equals(command, originalCommand))
                {
                    command.Parent = originalCommand;
                }

                if (handlingKernel is ProxyKernel &&
                    command.DestinationUri is null)
                {
                    command.DestinationUri = handlingKernel.KernelInfo.RemoteUri;
                }
            }

            return true;
        }

        private bool TryAdjustLanguageServiceCommandLinePositions(
            LanguageServiceCommand command, 
            KernelInvocationContext context, 
            out LanguageServiceCommand adjustedCommand)
        {
            var tree = SubmissionParser.Parse(command.Code, command.TargetKernelName);
            var rootNode = tree.GetRoot();
            var sourceText = SourceText.From(command.Code);
            var lines = sourceText.Lines;
            if (command.LinePosition.Line < 0
                || command.LinePosition.Line >= lines.Count
                || command.LinePosition.Character < 0
                || command.LinePosition.Character > lines[command.LinePosition.Line].Span.Length)
            {
                context.Fail(command, message: $"The specified position {command.LinePosition}");
                adjustedCommand = null;
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

                adjustedCommand = offsetLanguageServiceCommand;
            }
            else
            {
                adjustedCommand = null;
            }

            return true;
        }

        public FrontendEnvironment FrontendEnvironment
        {
            get => _frontendEnvironment ??=
                       ParentKernel?.FrontendEnvironment ??
                       new AutomationEnvironment();
            set => _frontendEnvironment = value;
        }

        protected bool IsDisposed => _disposables.IsDisposed;

        public IObservable<KernelEvent> KernelEvents => _kernelEvents;
      
        public string Name { get; }

        public KernelInfo KernelInfo => _kernelInfo;

        public IReadOnlyCollection<Command> Directives => SubmissionParser.Directives;

        public void AddDirective(Command command)
        {
            SubmissionParser.AddDirective(command);
            KernelInfo.SupportedDirectives.Add(new(command.Name));
        }

        public void RegisterCommandHandler<TCommand>(Func<TCommand, KernelInvocationContext, Task> handler)
            where TCommand : KernelCommand
        {
            RegisterCommandType<TCommand>();
            _dynamicHandlers[typeof(TCommand)] = (command, context) => handler((TCommand)command, context);
        }

        public void RegisterCommandType<TCommand>()
            where TCommand : KernelCommand
        {
            // QUESTION: (RegisterCommandType) why is this a separate gesture from RegisterCommand?
            if (_supportedCommandTypes.Add(typeof(TCommand)))
            {
                var defaultHandler = CreateDefaultHandlerForCommandType<TCommand>() ?? throw new InvalidOperationException("CreateDefaultHandlerForCommandType should not return null");
                
                _dynamicHandlers[typeof(TCommand)] = (command, context) => defaultHandler((TCommand)command, context);

                _kernelInfo.SupportedKernelCommands.Add(new(typeof(TCommand).Name));
            }
        }

        protected virtual Func<TCommand, KernelInvocationContext, Task> CreateDefaultHandlerForCommandType<TCommand>() where TCommand : KernelCommand
        {
            return (_,_) => Task.CompletedTask;
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
            using var disposable = new SerialDisposable();

            KernelInvocationContext context = null;

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.ShouldPublishCompletionEvent ??= true;

            context = KernelInvocationContext.Establish(command);

            // only subscribe for the root command 
            var currentCommandOwnsContext = CommandEqualityComparer.Instance.Equals(context.Command, command);

            if (currentCommandOwnsContext)
            {
                disposable.Disposable = context.KernelEvents.Subscribe(PublishEvent);

                if (cancellationToken != CancellationToken.None &&
                    cancellationToken != default)
                {
                    cancellationToken.Register(context.Cancel);
                }
            }

            if (TryPreprocessCommands(command, context, out var commands))
            {
                SetHandlingKernel(command, context);
          
                foreach (var c in commands)
                {
                    switch (c)
                    {
                        case Quit quit:
                            quit.SchedulingScope = SchedulingScope;
                            quit.TargetKernelName = Name;
                            await InvokePipelineAndCommandHandler(quit);
                            break;

                        case Cancel cancel:
                            cancel.SchedulingScope = SchedulingScope;
                            cancel.TargetKernelName = Name;
                            Scheduler.CancelCurrentOperation(inflight =>
                            {
                                context.Publish(new CommandCancelled(cancel, inflight));
                            });
                            await InvokePipelineAndCommandHandler(cancel);
                            break;

                        case RequestDiagnostics _:
                        {
                            if (_countOfLanguageServiceCommandsInFlight > 0)
                            {
                                context.CancelWithSuccess();
                                return context.Result;
                            }

                            if (_inFlightContext is { } inflight)
                            {
                                inflight.Complete(inflight.Command);
                            }

                            _inFlightContext = context;

                            await RunOnFastPath(context, c, cancellationToken);

                            _inFlightContext = null;
                        }
                            break;

                        case RequestHoverText _:
                        case RequestCompletions _:
                        case RequestSignatureHelp _:
                        {
                            if (_inFlightContext is { } inflight)
                            {
                                inflight.CancelWithSuccess();
                            }

                            Interlocked.Increment(ref _countOfLanguageServiceCommandsInFlight);

                            await RunOnFastPath(context, c, cancellationToken);

                            Interlocked.Decrement(ref _countOfLanguageServiceCommandsInFlight);
                        }
                            break;

                        case DisplayError _:
                        case DisplayValue _:
                        case RequestKernelInfo _:
                        case RequestValue _:
                        case RequestValueInfos _:
                        case UpdateDisplayedValue _:

                            await RunOnFastPath(context, c, cancellationToken);
                            break;

                        default:
                            await Scheduler.RunAsync(
                                               c,
                                               InvokePipelineAndCommandHandler,
                                               c.SchedulingScope.ToString(),
                                               cancellationToken: cancellationToken)
                                           .ContinueWith(t =>
                                           {
                                               if (t.IsCanceled)
                                               {
                                                   context.Cancel();
                                               }
                                           }, cancellationToken);
                            break;
                    }
                }
            }

            if (currentCommandOwnsContext)
            {
                context.Dispose();
            }

            return context.ResultFor(command);
        }

        internal SchedulingScope SchedulingScope
        {
            get
            {
                if (ParentKernel is null)
                {
                    return SchedulingScope.Parse(Name);
                }
                else
                {
                    return ParentKernel.SchedulingScope.Append($"{Name}");
                }
            }
        }

        private async Task RunOnFastPath(
            KernelInvocationContext context,
            KernelCommand command, 
            CancellationToken cancellationToken)
        {
            await RunDeferredCommandsAsync(context);

            await _fastPathScheduler.RunAsync(
                    command,
                    InvokePipelineAndCommandHandler,
                    command.SchedulingScope.ToString(),
                    cancellationToken: cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        context.Cancel();
                    }
                }, cancellationToken);
        }

        private async Task RunDeferredCommandsAsync(KernelInvocationContext context)
        {
            try
            {
                await SendAsync(
                    new UndeferScheduledCommands(
                        context.HandlingKernel.Name,
                        context.Command), context.CancellationToken);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private class UndeferScheduledCommands : AnonymousKernelCommand
        {
            public UndeferScheduledCommands(
                string targetKernelName, 
                KernelCommand parent) : base((_, _) =>
            {
                Log.Info("Undeferring commands ahead of {command}", parent);
                return Task.CompletedTask;
            }, targetKernelName: targetKernelName, parent: parent)
            {
            }

            public override string ToString() => $"Undefer commands ahead of {Parent}";
        }

        internal async Task<KernelCommandResult> InvokePipelineAndCommandHandler(KernelCommand command)
        {
            var context = KernelInvocationContext.Establish(command);

            try
            {
                SetHandlingKernel(command, context);

                await Pipeline.SendAsync(command, context);

                if (!CommandEqualityComparer.Instance.Equals(command, context.Command))
                {
                    context.Complete(command);
                }
                
                return context.ResultFor(command);
            }
            catch (Exception exception)
            {
                if (!context.IsComplete)
                {
                    context.Fail(command, exception);
                }

                throw;
            }
        }

        protected internal KernelScheduler<KernelCommand, KernelCommandResult> Scheduler
        {
            get
            {
                if (_commandScheduler is null)
                {
                    var scheduler = new KernelScheduler<KernelCommand, KernelCommandResult>();
                    RegisterForDisposal(scheduler);
                    SetScheduler(scheduler);
                }

                return _commandScheduler;
            }
        }

        protected internal void SetScheduler(KernelScheduler<KernelCommand, KernelCommandResult> scheduler)
        {
            _commandScheduler = scheduler;

            _commandScheduler.RegisterDeferredOperationSource(
                GetDeferredCommands, 
                InvokePipelineAndCommandHandler);
        }

        private IReadOnlyList<KernelCommand> GetDeferredCommands(KernelCommand command, string scope)
        {
            if (!command.SchedulingScope.Contains(SchedulingScope))
            {
                return Array.Empty<KernelCommand>();
            }

            var deferredCommands = new List<KernelCommand>();

            while (_deferredCommands.TryDequeue(out var kernelCommand))
            {
                kernelCommand.TargetKernelName = Name;
                kernelCommand.SchedulingScope = SchedulingScope;

                var currentInvocationContext = KernelInvocationContext.Current;

                if (TryPreprocessCommands(kernelCommand, currentInvocationContext, out var commands))
                {
                    deferredCommands.AddRange(commands);
                }
            }

            return deferredCommands;
        }

        public virtual Task HandleAsync(
            RequestKernelInfo command,
            KernelInvocationContext context)
        {
            context.Publish(new KernelInfoProduced(KernelInfo, command));

            return Task.CompletedTask;
        }

        public virtual Task HandleAsync(
            RequestValue command,
            KernelInvocationContext context)
        {
            if (this is ISupportGetValue supportsGetValue)
            {
                if (supportsGetValue.TryGetValue(command.Name, out object value))
                {
                    if (value is { })
                    {
                        var valueType = value.GetType();
                        var formatter = Formatter.GetPreferredFormatterFor(valueType, command.MimeType);

                        using var writer = new StringWriter(CultureInfo.InvariantCulture);
                        formatter.Format(value, writer);
                        var formatted = new FormattedValue(command.MimeType, writer.ToString());
                        context.Publish(new ValueProduced(value, command.Name, formatted, command));
                    }
                    else
                    {
                        var formatted = new FormattedValue(command.MimeType, "null");

                        context.Publish(new ValueProduced(value, command.Name, formatted, command));
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Cannot find value named: {command.Name}");
                }
            }
            else
            {
                context.Fail(command, message: $"Value '{command.Name}' not found in kernel {this}");
            }

            return Task.CompletedTask;
        }

        public virtual Task HandleAsync(
            RequestValueInfos command,
            KernelInvocationContext context)
        {
            if (context.HandlingKernel is ISupportGetValue supportsGetValue)
            {
                context.Publish(new ValueInfosProduced(supportsGetValue.GetValueInfos(), command));
                return Task.CompletedTask;
            }

            throw new InvalidOperationException($"Kernel {context.HandlingKernel.Name} doesn't support command {nameof(RequestValueInfos)}");
        }

        private protected bool CanHandle(KernelCommand command)
        {
            if (command.TargetKernelName is not null &&
                command.TargetKernelName != Name)
            {
                return false;
            }

            if (command.DestinationUri is not null)
            {
                if (KernelInfo.RemoteUri != command.DestinationUri)
                {
                    return false;
                }
            }

            return SupportsCommand(command);
        }

        private protected virtual Kernel GetHandlingKernel(
            KernelCommand command,
            KernelInvocationContext context)
        {
            if (CanHandle(command))
            {
                return this;
            }
            else
            {
                context.Fail(command, new CommandNotSupportedException(command, this));

                return null;
            }
        }

        protected internal void PublishEvent(KernelEvent kernelEvent)
        {
            if (kernelEvent is null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            _kernelEvents.OnNext(kernelEvent);
        }

        public void RegisterForDisposal(Action dispose) => RegisterForDisposal(Disposable.Create(dispose));

        public void RegisterForDisposal(IDisposable disposable)
        {
            if (disposable is null)
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

            var result = directiveNode.GetDirectiveParseResult();
            if (result.CommandResult.Command == ChooseKernelDirective)
            {
                return result.GetCompletions()
                             .Select(s => SubmissionParser.CompletionItemFor(s.Label, result));
            }

            var allCompletions = new List<CompletionItem>();
            var topDirectiveParser = SubmissionParser.GetDirectiveParser();
            var prefix = topDirectiveParser.Configuration.RootCommand.Name + " ";
            requestPosition += prefix.Length;

            foreach (var parser in directiveParsers)
            {
                var effectiveText = $"{prefix}{directiveNode.Text}";

                var parseResult = parser.Parse(effectiveText);

                var suggestions = parseResult.GetCompletions(requestPosition);

                var completions = suggestions
                                  .Select(s => SubmissionParser.CompletionItemFor(s.Label, parseResult))
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

        private void TrySetHandler(
            KernelCommand command,
            KernelInvocationContext context)
        {
            if (command.Handler is null)
            {
                if (TrySetDynamicHandler(command))
                {
                    return;
                }

                switch (command, this)
                {
                    case (SubmitCode submitCode, IKernelCommandHandler<SubmitCode> submitCodeHandler):
                        SetHandler(submitCode, submitCodeHandler);
                        break;

                    case (RequestCompletions {LanguageNode: DirectiveNode} rq, _):
                        rq.Handler = (_, _) => HandleRequestCompletionsAsync(rq, context);
                        break;

                    case (RequestCompletions requestCompletion, IKernelCommandHandler<RequestCompletions>
                        requestCompletionHandler):
                        SetHandler(requestCompletion, requestCompletionHandler);
                        break;

                    case (RequestDiagnostics requestDiagnostics, IKernelCommandHandler<RequestDiagnostics>
                        requestDiagnosticsHandler):
                        SetHandler(requestDiagnostics, requestDiagnosticsHandler);
                        break;

                    case (RequestHoverText hoverCommand, IKernelCommandHandler<RequestHoverText> requestHoverTextHandler):
                        SetHandler(hoverCommand, requestHoverTextHandler);
                        break;

                    case (RequestSignatureHelp requestSignatureHelp, IKernelCommandHandler<RequestSignatureHelp>
                        requestSignatureHelpHandler):
                        SetHandler(requestSignatureHelp, requestSignatureHelpHandler);
                        break;
                    
                    case (RequestValue requestValue, IKernelCommandHandler<RequestValue>
                        requestValueHandler):
                        SetHandler(requestValue, requestValueHandler);
                        break; 

                    case (RequestValueInfos requestValueInfos, IKernelCommandHandler<RequestValueInfos>
                        requestValueInfosHandler):
                        SetHandler(requestValueInfos, requestValueInfosHandler);
                        break;

                    case (ChangeWorkingDirectory changeWorkingDirectory, IKernelCommandHandler<ChangeWorkingDirectory> changeWorkingDirectoryHandler):
                        SetHandler(changeWorkingDirectory, changeWorkingDirectoryHandler);
                        break;

                    case (RequestKernelInfo requestKernelInfo, IKernelCommandHandler<RequestKernelInfo> requestKernelInfoHandler):
                        SetHandler(requestKernelInfo, requestKernelInfoHandler);
                        break;
                }
            }
        }

        private bool TrySetDynamicHandler(KernelCommand command)
        {
            if (_dynamicHandlers.TryGetValue(command.GetType(), out var handler))
            {
                command.Handler = handler;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void SetHandler<T>(T command, IKernelCommandHandler<T> handler)
            where T : KernelCommand =>
            command.Handler = (_, context) =>
                handler.HandleAsync(command, context);

        protected virtual void SetHandlingKernel(
            KernelCommand command,
            KernelInvocationContext context) => context.HandlingKernel = this;

        public void Dispose() => _disposables.Dispose();

        public virtual ChooseKernelDirective ChooseKernelDirective => _chooseKernelDirective ??= new(this);

        internal virtual bool AcceptsUnknownDirectives => false;

        public bool SupportsCommand(KernelCommand command)
        {
            if (command.Handler is not null)
            {
                return true;
            }

            if (KernelInfo.SupportsCommand(command.GetType().Name))
            {
                return true;
            }

            switch (command)
            {
                case AnonymousKernelCommand:
                    return true;
                case DirectiveCommand:
                    return true;
                case Quit:
                    return true;
                case Cancel:
                    return true;
                case DisplayValue:
                    return true;
                case RequestCompletions:
                    return true;
                default:
                    return false;
            }
        }

        public bool SupportsCommandType(Type commandType)
        {
            if (KernelInfo.SupportsCommand(commandType.Name))
            {
                return true;
            }

            if (commandType == typeof(AnonymousKernelCommand))
            {
                return true;
            }

            if (commandType == typeof(DirectiveCommand))
            {
                return true;
            }

            if (commandType == typeof(DisplayValue))
            {
                return true;
            }

            return false;
        }

        public virtual IKernelValueDeclarer GetValueDeclarer() => KernelValueDeclarer.Default;

        public override string ToString()
        {
            var value = $"{base.ToString()}: {Name}";

            var kernelInfoUri = KernelInfo.Uri;
            if (kernelInfoUri is { } uri)
            {
                string remoteUri = null;
                if (KernelInfo.RemoteUri is not null)
                {
                    remoteUri += $" -> {KernelInfo.RemoteUri}";
                }

                value += $" ({uri}{remoteUri})";
            }

            return value;
        }
    }
}
