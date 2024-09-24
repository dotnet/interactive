// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CSharp.RuntimeBinder;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Parsing;
using Pocket;

using static Pocket.Logger<Microsoft.DotNet.Interactive.Kernel>;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive;

public abstract partial class Kernel :
    IKernelCommandHandler<RequestKernelInfo>,
    IDisposable
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<Type>> _declaredHandledCommandTypesByKernelType = new();
    private readonly HashSet<Type> _supportedCommandTypes;

    private readonly Subject<KernelEvent> _kernelEvents = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly ConcurrentDictionary<Type, KernelCommandInvocation> _dynamicHandlers = new();
    private readonly ConcurrentDictionary<string, KernelCommandInvocation> _directiveHandlers = new();
    private KernelScheduler<KernelCommand, KernelCommandResult> _commandScheduler;
    private readonly ImmediateScheduler<KernelCommand, KernelCommandResult> _fastPathScheduler = new();
    private FrontendEnvironment _frontendEnvironment;
    private KernelSpecifierDirective _kernelSpecifierDirective;
    private readonly ConcurrentQueue<KernelCommand> _deferredCommands = new();
    private KernelInvocationContext _inFlightContext;
    private int _countOfLanguageServiceCommandsInFlight = 0;
    private readonly KernelInfo _kernelInfo;

    protected Kernel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        RootKernel = this;

        Name = name;

        SubmissionParser = new SubmissionParser(this);

        _disposables.Add(Disposable.Create(() => _kernelEvents.OnCompleted()));

        Pipeline = new KernelCommandPipeline(this);

        _supportedCommandTypes = new HashSet<Type>(
            _declaredHandledCommandTypesByKernelType
                .GetOrAdd(
                    GetType(),
                    GetImplementedCommandHandlerTypesFor));

        _kernelInfo = InitializeKernelInfo(name);

        var counter = _kernelEvents.Subscribe(IncrementSubmissionCount);

        RegisterForDisposal(counter);

        void IncrementSubmissionCount(KernelEvent e)
        {
            if (e is KernelCommandCompletionEvent)
            {
                if (e.Command is SubmitCode)
                {
                    if (e.Command.RoutingSlip.Count == 0 || e.Command.RoutingSlip.Contains(KernelInfo.Uri, true))
                    {
                        SubmissionCount++;
                    }
                }
            }
        }
    }

    [Obsolete("This constructor has been deprecated.  Please use the other constructor and directly set any remaining properties directly on the " + nameof(KernelInfo) + " property.")]
    protected Kernel(
        string name,
        string languageName,
        string languageVersion)
        : this(name)
    {
        KernelInfo.LanguageName = languageName;
        KernelInfo.LanguageVersion = languageVersion;
    }

    private KernelInfo InitializeKernelInfo(string name)
    {
        var kernelInfo = new KernelInfo(name);

        foreach (var commandInfo in _supportedCommandTypes.Select(t => new KernelCommandInfo(t.Name)))
        {
            kernelInfo.SupportedKernelCommands.Add(commandInfo);
        }

        return kernelInfo;
    }

    internal KernelCommandPipeline Pipeline { get; }

    public CompositeKernel ParentKernel { get; internal set; }

    public Kernel RootKernel { get; internal set; }

    public int SubmissionCount { get; private set; }

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

        _deferredCommands.Enqueue(command);
    }

    private async Task<(bool successful, IReadOnlyList<KernelCommand> commands)> TrySplitCommand(
        KernelCommand originalCommand,
        KernelInvocationContext context)
    {
        IReadOnlyList<KernelCommand> commands = null;

        switch (originalCommand)
        {
            case SubmitCode { SyntaxNode: null } submitCode:
                commands = await SubmissionParser.SplitSubmission(submitCode);
                break;

            case RequestDiagnostics { SyntaxNode: null } requestDiagnostics:
                commands = await SubmissionParser.SplitSubmission(requestDiagnostics);
                break;

            case LanguageServiceCommand { SyntaxNode: null } languageServiceCommand:
                if (!TryAdjustLanguageServiceCommandLinePositions(languageServiceCommand, context, out var adjustedCommand))
                {
                    return (false, null);
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
                context.Fail(command, new CommandNotSupportedException(command.GetType(), this));
                return (false, null);
            }

            if (command.DestinationUri is not null &&
                handlingKernel.KernelInfo.Uri is { } uri &&
                command.DestinationUri == uri)
            {
                command.SchedulingScope = handlingKernel.SchedulingScope;
                command.TargetKernelName = handlingKernel.Name;
            }

            command.SchedulingScope ??= handlingKernel.SchedulingScope;
            command.TargetKernelName ??= handlingKernel.Name;

            if (!command.Equals(originalCommand))
            {
                command.SetParent(originalCommand, true);
            }

            if (handlingKernel is ProxyKernel &&
                command.DestinationUri is null)
            {
                command.DestinationUri = handlingKernel.KernelInfo.RemoteUri;
            }
        }

        return (true, commands);
    }

    private bool TryAdjustLanguageServiceCommandLinePositions(
        LanguageServiceCommand command,
        KernelInvocationContext context,
        out LanguageServiceCommand adjustedCommand)
    {
        var tree = SubmissionParser.Parse(command.Code, command.TargetKernelName);
        var rootNode = tree.RootNode;
        var sourceText = tree.RootNode.SourceText;
        var lines = sourceText.Lines;

        if (command.LinePosition.Line < 0
            || command.LinePosition.Line >= lines.Count
            || command.LinePosition.Character < 0
            || command.LinePosition.Character > lines[command.LinePosition.Line].Span.Length)
        {
            context.Fail(command, message: $"The specified position {command.LinePosition} is invalid.");
            adjustedCommand = null;
            return false;
        }

        // TextSpan.Contains only checks `[start, end)`, but we need to allow for `[start, end]`
        var absolutePosition = tree.RootNode.SourceText.Lines.GetPosition(command.LinePosition.ToCodeAnalysisLinePosition());

        if (absolutePosition > 0 &&
            absolutePosition < rootNode.FullSpan.Length &&
            char.IsWhiteSpace(rootNode.FullText[absolutePosition]))
        {
            absolutePosition--;
        }

        if (rootNode.FindNode(absolutePosition)?.AncestorsAndSelf().OfType<TopLevelSyntaxNode>().FirstOrDefault() is { } node)
        {
            var nodeStartLine = sourceText.Lines.GetLinePosition(node.Span.Start).Line;
            var offsetNodeLine = command.LinePosition.Line - nodeStartLine;
            var position = command.LinePosition with { Line = offsetNodeLine };

            // create new command
            var offsetLanguageServiceCommand = command.AdjustForCommandSplit(
                node,
                position,
                absolutePosition);

            offsetLanguageServiceCommand.TargetKernelName = node.TargetKernelName;

            adjustedCommand = offsetLanguageServiceCommand;
        }
        else
        {
            adjustedCommand = null;
            // need to return false to notify caller of failure
            // otherwise caller assumes out param is valid ref
            return false;
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

    public void AddDirective(KernelActionDirective directive, KernelCommandInvocation handler)
    {
        KernelInfo.SupportedDirectives.Add(directive);

        RegisterDirectiveCommandHandler(directive, handler);

        SubmissionParser.ResetParser();
    }

    public void AddDirective<TCommand>(KernelActionDirective directive, Func<TCommand, KernelInvocationContext, Task> handler)
        where TCommand : KernelCommand
    {
        if (directive.KernelCommandType is null)
        {
            directive.KernelCommandType = typeof(TCommand);
        }
        else if (directive.KernelCommandType != typeof(TCommand))
        {
            throw new ArgumentException($"{nameof(directive)}.{nameof(KernelActionDirective.KernelCommandType)} must be set to {typeof(TCommand)}.");
        }

        // Don't add subcommands to the KernelInfo
        if (directive.Parent is null)
        {
            KernelInfo.SupportedDirectives.Add(directive);
        }

        RegisterCommandHandler(handler);

        SubmissionParser.ResetParser();

        KernelCommandEnvelope.RegisterCommand<TCommand>();
    }

    public virtual KernelSpecifierDirective KernelSpecifierDirective => _kernelSpecifierDirective ??= new($"#!{Name}", Name);

    private void RegisterDirectiveCommandHandler(
        KernelActionDirective directive,
        KernelCommandInvocation handler)
    {
        var fullDirectiveName = FullDirectiveName(directive);

        _directiveHandlers[fullDirectiveName] = handler;
    }

    private static string FullDirectiveName(KernelActionDirective directive) =>
        directive.Parent is { } parent
            ? $"{parent.Name} {directive.Name}"
            : directive.Name;

    public void RegisterCommandHandler<TCommand>(Func<TCommand, KernelInvocationContext, Task> handler)
        where TCommand : KernelCommand
    {
        RegisterCommandType<TCommand>();
        _dynamicHandlers[typeof(TCommand)] = (command, context) => handler((TCommand)command, context);
    }

    public void RegisterCommandType<TCommand>()
        where TCommand : KernelCommand
    {
        // FIX: (RegisterCommandType) consider always automatically calling KernelCommandEnvelope.RegisterCommand<TCommand>();
        // FIX: (RegisterCommandType) why is this a separate gesture from RegisterCommand? Does it even need to be public?
        if (_supportedCommandTypes.Add(typeof(TCommand)))
        {
            var defaultHandler = CreateDefaultHandlerForCommandType<TCommand>() ?? throw new InvalidOperationException("CreateDefaultHandlerForCommandType should not return null");

            _dynamicHandlers[typeof(TCommand)] = (command, context) => defaultHandler((TCommand)command, context);

            _kernelInfo.SupportedKernelCommands.Add(new(typeof(TCommand).Name));
        }
    }

    protected virtual Func<TCommand, KernelInvocationContext, Task> CreateDefaultHandlerForCommandType<TCommand>() where TCommand : KernelCommand
    {
        return EmptyHandler;

        Task EmptyHandler(TCommand _, KernelInvocationContext __) => Task.CompletedTask;
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
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        using var disposable = new SerialDisposable();

        command.ShouldPublishCompletionEvent ??= true;

        var context = KernelInvocationContext.GetOrCreateAmbientContext(command, GetKernelHost()?.ContextsByRootToken);

        // only subscribe for the root command 
        var currentCommandOwnsContext = ReferenceEquals(context.Command, command);

        if (currentCommandOwnsContext)
        {
            disposable.Disposable = context.KernelEvents.Subscribe(PublishEvent);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(context.Cancel);
            }
        }

        if (await TrySplitCommand(command, context) is { successful: true, commands: { } commands })
        {
            SetHandlingKernel(command, context);

            foreach (var c in commands)
            {
                switch (c)
                {
                    case Quit quit:
                        quit.SchedulingScope = SchedulingScope;
                        quit.TargetKernelName = Name;
                        Scheduler.CancelCurrentOperation();
                        await InvokePipelineAndCommandHandler(quit);
                        break;

                    case Cancel cancel:
                        cancel.SchedulingScope = SchedulingScope;
                        cancel.TargetKernelName = Name;
                        Scheduler.CancelCurrentOperation();
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
                    case SendValue _:
                    case UpdateDisplayedValue _:

                        await RunOnFastPath(context, c, cancellationToken, skipDeferredCommands: true);
                        break;

                    default:
                        if (!context.IsComplete)
                        {
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
                        }
                        break;
                }
            }
        }

        if (currentCommandOwnsContext)
        {
            try
            {
                await Scheduler.IdleAsync();
            }
            catch (InvalidOperationException ex)
            {
                Log.Warning($"Error while awaiting idle after sending {command}", ex);
                throw;
            }
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
        CancellationToken cancellationToken,
        bool skipDeferredCommands = false)
    {
        if (!skipDeferredCommands)
        {
            await RunDeferredCommandsAsync(context);
        }

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
            var undeferScheduledCommands = new UndeferScheduledCommands(
                context.HandlingKernel.Name,
                context.Command);

            await SendAsync(
                undeferScheduledCommands,
                context.CancellationToken);
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
                Log.Info("Undeferring commands ahead of '{command}'", parent);
                return Task.CompletedTask;
            }, targetKernelName: targetKernelName)
        {
            SetParent(parent);
        }

        public override string ToString() => $"Undefer commands ahead of {Parent}";
    }
    private KernelHost GetKernelHost()
    {
        return RootKernel is CompositeKernel { Host: { } kernelHost } ? kernelHost : null;
    }

    internal async Task<KernelCommandResult> InvokePipelineAndCommandHandler(KernelCommand command)
    {
        var context = KernelInvocationContext.GetOrCreateAmbientContext(command, GetKernelHost()?.ContextsByRootToken);

        try
        {
            SetHandlingKernel(command, context);

            await Pipeline.SendAsync(command, context);

            if (!command.Equals(context.Command))
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
                var scheduler = new KernelCommandScheduler();
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
            GetDeferredCommandsAsync,
            InvokePipelineAndCommandHandler);
    }

    private bool IsInSchedulingScope(KernelCommand command)
    {
        if (command.SchedulingScope is null)
        {
            return false;
        }

        return command.SchedulingScope.Contains(SchedulingScope);
    }

    private async Task<IReadOnlyList<KernelCommand>> GetDeferredCommandsAsync(KernelCommand command, string scope)
    {
        if (!IsInSchedulingScope(command))
        {
            return Array.Empty<KernelCommand>();
        }

        var deferredCommands = new List<KernelCommand>();
        while (_deferredCommands.TryDequeue(out var kernelCommand))
        {
            var currentInvocationContext = KernelInvocationContext.Current;
            kernelCommand.TargetKernelName = Name;
            kernelCommand.SchedulingScope = SchedulingScope;
            kernelCommand.SetParent(currentInvocationContext.Command);

            if (await TrySplitCommand(kernelCommand, currentInvocationContext) is { successful: true, commands: { } commands })
            {
                deferredCommands.AddRange(commands);
            }
        }

        return deferredCommands;
    }

    Task IKernelCommandHandler<RequestKernelInfo>.HandleAsync(
        RequestKernelInfo command,
        KernelInvocationContext context) =>
        HandleRequestKernelInfoAsync(command, context);

    private protected virtual Task HandleRequestKernelInfoAsync(RequestKernelInfo command, KernelInvocationContext context)
    {
        context.Publish(new KernelInfoProduced(KernelInfo, command));
        return Task.CompletedTask;
    }

    private protected bool CanHandle(KernelCommand command)
    {
        if (!KernelInfo.IsProxy &&
            command.TargetKernelName is not null &&
            command.TargetKernelName != Name)
        {
            return false;
        }

        if (command.DestinationUri is not null)
        {
            if (KernelInfo.Uri != command.DestinationUri &&
                KernelInfo.RemoteUri != command.DestinationUri)
            {
                return false;
            }
        }

        return SupportsCommand(command);
    }

    private protected bool HasDynamicHandlerFor(KernelCommand command)
    {
        return _dynamicHandlers.ContainsKey(command.GetType());
    }

    private protected virtual Kernel GetHandlingKernel(
        KernelCommand command,
        KernelInvocationContext context)
    {
        if (CanHandle(command))
        {
            return this;
        }

        return null;
    }

    protected internal void PublishEvent(KernelEvent kernelEvent)
    {
        if (kernelEvent is null)
        {
            throw new ArgumentNullException(nameof(kernelEvent));
        }

        if (!kernelEvent.RoutingSlip.Contains(KernelInfo.Uri))
        {
            kernelEvent.StampRoutingSlipAndLog(KernelInfo.Uri);
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

    private async Task PublishDirectiveCompletionsAsync(
        RequestCompletions command,
        KernelInvocationContext context)
    {
        if (command.SyntaxNode is DirectiveNode directiveNode)
        {
            var allCompletions = await directiveNode.GetCompletionsAtPositionAsync(command.OriginalPosition);

            var completions = allCompletions
                              .Distinct(CompletionItemComparer.Instance)
                              .ToArray();

            var upToCursor = directiveNode.FullText[..command.LinePosition.Character];

            var indexOfPreviousSpace =
                Math.Max(
                    0,
                    upToCursor.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase) + 1);

            var resultRange = new LinePositionSpan(
                command.LinePosition with { Character = indexOfPreviousSpace },
                command.LinePosition);

            context.Publish(
                new CompletionsProduced(
                    completions.ToArray(), command, resultRange));
        }
    }

    private Task PublishDirectiveHoverTextAsync(
        RequestHoverText command,
        KernelInvocationContext context)
    {
        if (command.SyntaxNode is DirectiveNode directiveNode)
        {
            string hoverText = null;

            if (!directiveNode.TryGetDirective(out var directive))
            {
                return Task.CompletedTask;
            }

            var node = directiveNode.FindNode(command.OriginalPosition);

            switch (node)
            {
                case DirectiveNameNode { Parent: DirectiveSubcommandNode subcommandNode }:
                    if (subcommandNode.TryGetSubcommand(out var subcommandDirective))
                    {
                        hoverText = BuildHoverText(subcommandDirective);
                    }

                    break;

                case DirectiveNameNode _:
                    hoverText = BuildHoverText(directive);

                    break;

                case DirectiveParameterNameNode directiveParameterNameNode:
                    if (directiveParameterNameNode.Parent is DirectiveParameterNode pn &&
                        pn.TryGetParameter(out var parameter))
                    {
                        hoverText = parameter.Description;
                    }

                    break;
            }

            if (hoverText is not null)
            {
                var linePosition = new LinePosition(command.LinePosition.Line, command.LinePosition.Character);

                var linePositionSpan = new LinePositionSpan(
                    linePosition,
                    linePosition);

                context.Publish(
                    new HoverTextProduced(
                        command,
                        [new FormattedValue("text/markdown", hoverText)],
                        linePositionSpan));
            }
        }

        return Task.CompletedTask;

        static string BuildHoverText(KernelDirective directive)
        {
            var sb = new StringBuilder();

            sb.AppendLine(directive.Description);

            if (directive.Parameters.Any())
            {
                sb.AppendLine();

                sb.AppendLine("| <span>Parameter&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</span> | Description  |");
                sb.AppendLine("| :----     | :----        |");

                foreach (var parameter in directive.Parameters.OrderByDescending(p => p.Required).ThenBy(p => p.Name))
                {
                    WriteParameterRow(sb, parameter);
                }
            }

            return sb.ToString();
        }

        static void WriteParameterRow(StringBuilder sb, KernelDirectiveParameter parameter)
        {
            sb.Append("|");

            sb.Append("*");
            if (parameter.Required)
            {
                sb.Append("*");
            }
            sb.Append(parameter.Name);
            sb.Append("*");
            if (parameter.Required)
            {
                sb.Append("*");
            }

            sb.Append("|");

            sb.Append(parameter.Description);
                    
            sb.AppendLine("|");
        }
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

                case (RequestCompletions { SyntaxNode: DirectiveNode } rq, _):
                    rq.Handler = (_, _) => PublishDirectiveCompletionsAsync(rq, context);
                    break;
                
                case (RequestHoverText { SyntaxNode: DirectiveNode } rq, _):
                    rq.Handler = (_, _) => PublishDirectiveHoverTextAsync(rq, context);
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

                case (SendValue sendValue, IKernelCommandHandler<SendValue>
                    sendValueHandler):
                    SetHandler(sendValue, sendValueHandler);
                    break;

                case (RequestValueInfos requestValueInfos, IKernelCommandHandler<RequestValueInfos>
                    requestValueInfosHandler):
                    SetHandler(requestValueInfos, requestValueInfosHandler);
                    break;

                case (RequestKernelInfo requestKernelInfo, IKernelCommandHandler<RequestKernelInfo> requestKernelInfoHandler):
                    SetHandler(requestKernelInfo, requestKernelInfoHandler);
                    break;

                case (Cancel cancel, _):
                    break;

                case (DirectiveCommand directiveCommand, _):
                    TrySetDirectiveHandler(directiveCommand);

                    break;

                default:
                    // for command types defined outside this assembly, we can dynamically assign the handler
                    if (command.GetType().IsPublic)
                    {
                        try
                        {
                            SetHandler((dynamic)command, (dynamic)this);
                        }
                        catch (RuntimeBinderException)
                        {
                        }
                    }

                    break;
            }
        }
    }

    private bool TrySetDirectiveHandler(DirectiveCommand command)
    {
        var fullDirectiveName = command.DirectiveNode.GetInvokedCommandPath();

        if (_directiveHandlers.TryGetValue(fullDirectiveName, out var handler))
        {
            command.Handler = handler;
            return true;
        }

        if (ParentKernel is { } parent &&
            parent._directiveHandlers.TryGetValue(fullDirectiveName, out handler))
        {
            command.Handler = handler;
            return true;
        }

        return false;
    }

    private bool TrySetDynamicHandler(KernelCommand command)
    {
        if (_dynamicHandlers.TryGetValue(command.GetType(), out var handler))
        {
            command.Handler = handler;
            return true;
        }


        return false;
    }

    private static void SetHandler<T>(T command, IKernelCommandHandler<T> handler)
        where T : KernelCommand =>
        command.Handler = (_, context) =>
            handler.HandleAsync(command, context);

    protected virtual void SetHandlingKernel(
        KernelCommand command,
        KernelInvocationContext context) => context.HandlingKernel = this;

    public void Dispose() => _disposables.Dispose();

    internal virtual bool AcceptsUnknownDirectives => false;

    internal bool SupportsCommand(KernelCommand command)
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

    protected async Task SetValueAsync(
        SendValue command,
        KernelInvocationContext context,
        SetValueAsyncDelegate onSetValueAsync)
    {
        object value = null;

        if (command.Value is not null)
        {
            switch (command.Value)
            {
                case Type { IsPublic: true } t:
                    value = t;
                    break;

                default:
                    value = command.Value;
                    break;
            }
        }

        if (value is null)
        {
            if (command.FormattedValue.MimeType is JsonFormatter.MimeType)
            {
                var jsonDoc = JsonDocument.Parse(command.FormattedValue.Value);

                value = jsonDoc.RootElement.ValueKind switch
                {
                    JsonValueKind.Object => jsonDoc,
                    JsonValueKind.Array => jsonDoc,

                    JsonValueKind.Undefined => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.String => jsonDoc.Deserialize<string>(),
                    JsonValueKind.Number => jsonDoc.Deserialize<double>(),

                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            else
            {
                value = command.FormattedValue.Value;
            }
        }

        await onSetValueAsync(command.Name, value);
    }

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