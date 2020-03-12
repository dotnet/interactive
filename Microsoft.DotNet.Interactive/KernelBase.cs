// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive
{
    public abstract class KernelBase : IKernel
    {
        private readonly Subject<IKernelEvent> _kernelEvents = new Subject<IKernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly SubmissionParser _submissionParser = new SubmissionParser();
        private readonly ConcurrentQueue<IKernelCommand> _deferredCommands = new ConcurrentQueue<IKernelCommand>();
        private readonly ConcurrentDictionary<Type, object> _properties = new ConcurrentDictionary<Type, object>();

        protected KernelBase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;

            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            AddCaptureConsoleMiddleware();

            AddSetKernelMiddleware();

            AddDirectiveMiddlewareAndCommonCommandHandlers();

            _disposables.Add(_kernelEvents);
        }

        internal KernelCommandPipeline Pipeline { get; }

        internal CompositeKernel ParentKernel { get; set; }

        public void AddMiddleware(
            KernelCommandPipelineMiddleware middleware,
            [CallerMemberName] string caller = null) => Pipeline.AddMiddleware(middleware, caller);

        public void DeferCommand(IKernelCommand command)
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
                (command, context, next) =>
                {
                    return command switch
                    {
                        SubmitCode submitCode =>
                        HandleDirectivesAndSubmitCode(
                            submitCode,
                            context,
                            next),

                        _ => next(command, context)
                    };
                });
        }

        private async Task SetKernel(IKernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next)
        {
            SetHandlingKernel(command, context);

            var previousKernel = context.CurrentKernel;

            context.CurrentKernel = this;

            await next(command, context);

            context.CurrentKernel = previousKernel;
        }

        private async Task HandleDirectivesAndSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext context,
            KernelPipelineContinuation continueOnCurrentPipeline)
        {
            var commands = _submissionParser.SplitSubmission(submitCode);

            if (!commands.Contains(submitCode))
            {
                context.CommandToSignalCompletion = commands.Last();
            }

            foreach (var command in commands)
            {
                if (context.IsComplete)
                {
                    break;
                }

                if (command == submitCode)
                {
                    // no new context is needed
                    await continueOnCurrentPipeline(submitCode, context);
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
                            var kernel = context.HandlingKernel;

                            if (kernel == this)
                            {
                                var c = KernelInvocationContext.Establish(command);

                                await continueOnCurrentPipeline(command, c);
                            }
                            else
                            {
                                // forward to next kernel
                                await kernel.SendAsync(command);
                            }

                            break;
                    }
                }
            }
        }

        public FrontendEnvironment FrontendEnvironment
        {
            get => _frontendEnvironment ??=
                       ParentKernel?.FrontendEnvironment ??
                       new AutomationEnvironment();
            set => _frontendEnvironment = value;
            
        }

        public IObservable<IKernelEvent> KernelEvents => _kernelEvents;

        public string Name { get; set; }

        public IReadOnlyCollection<ICommand> Directives => _submissionParser.Directives;

        public abstract bool TryGetVariable(string name, out object value);

        public void AddDirective(Command command) => _submissionParser.AddDirective(command); 
        
        public virtual Task<JObject> LspMethod(string methodName, JObject request)
        {
            return Task.FromResult<JObject>(null);
        }

        private class KernelOperation
        {
            public KernelOperation(IKernelCommand command, TaskCompletionSource<IKernelCommandResult> taskCompletionSource)
            {
                Command = command;
                TaskCompletionSource = taskCompletionSource;
            }

            public IKernelCommand Command { get; }

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
            IKernelCommand command,
            KernelInvocationContext context)
        {
            SetHandler(command, context);
            await command.InvokeAsync(context);
        }

        private readonly ConcurrentQueue<KernelOperation> _commandQueue =
            new ConcurrentQueue<KernelOperation>();

        private FrontendEnvironment _frontendEnvironment;

        public Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken)
        {
            return SendAsync(command, cancellationToken, null);
        }

        internal Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
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

        protected void PublishEvent(IKernelEvent kernelEvent)
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
   
        protected abstract Task HandleSubmitCode(
            SubmitCode command, 
            KernelInvocationContext context);

        protected abstract Task HandleRequestCompletion(
            RequestCompletion command, 
            KernelInvocationContext context);

        private protected void SetHandler(
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
                            submitCode.Handler = (_, invocationContext) =>
                            {
                                return HandleSubmitCode(submitCode, context);
                            };
                            break;

                        case RequestCompletion requestCompletion:
                            requestCompletion.Handler = (_, invocationContext) =>
                            {
                                return HandleRequestCompletion(requestCompletion, invocationContext);
                            };
                            break;
                    }
                }
            }
        }

        protected virtual void SetHandlingKernel(
            IKernelCommand command,
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