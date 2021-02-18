// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public class KernelCommandScheduler
    {
        private readonly ConcurrentQueue<(KernelCommand command, Kernel kernel)> _deferredCommands = new();

        private readonly ConcurrentQueue<KernelOperation> _commandQueue = new();

        public Task<KernelCommandResult> Schedule(KernelCommand command, Kernel kernel)
        {
            return Schedule(command, kernel, CancellationToken.None, () => { });
        }


        public Task<KernelCommandResult> Schedule(KernelCommand command, Kernel kernel, CancellationToken cancellationToken, Action onDone)
        {


            switch (command)
            {
                case Cancel _:
                    CancelCommands();
                    break;
                default:
                    UndeferCommandsFor(kernel);
                    break;
            }

            var kernelCommandResultSource = new TaskCompletionSource<KernelCommandResult>();

            var operation = new KernelOperation(command, kernelCommandResultSource, kernel);

            _commandQueue.Enqueue(operation);

            ProcessCommandQueue(_commandQueue, cancellationToken, onDone);

            return kernelCommandResultSource.Task;
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
                    AsyncContext.Id = currentOperation.AsyncContextId;

                    await ExecuteCommand(currentOperation);

                    ProcessCommandQueue(commandQueue, cancellationToken, onDone);
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                onDone?.Invoke();
            }
        }

        private async Task ExecuteCommand(KernelOperation operation)
        {
            var context = KernelInvocationContext.Establish(operation.Command);

            // only subscribe for the root command 
            using var _ =
                context.Command == operation.Command
                    ? context.KernelEvents.Subscribe(operation.Kernel.PublishEvent)
                    : Disposable.Empty;

            try
            {
                await operation.Kernel.Pipeline.SendAsync(operation.Command, context);

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
        private class KernelOperation
        {
            public KernelOperation(
                KernelCommand command,
                TaskCompletionSource<KernelCommandResult> taskCompletionSource,
                Kernel kernel)
            {
                Command = command;
                TaskCompletionSource = taskCompletionSource;
                Kernel = kernel;

                AsyncContext.TryEstablish(out var id);
                AsyncContextId = id;
            }

            public KernelCommand Command { get; }

            public TaskCompletionSource<KernelCommandResult> TaskCompletionSource { get; }
            public Kernel Kernel { get; }

            public int AsyncContextId { get; }
        }

        public void DeferCommand(KernelCommand command, Kernel kernel)
        {
            _deferredCommands.Enqueue((command, kernel));
        }

        internal Task RunDeferredCommandsAsync(Kernel kernel)
        {
            var tcs = new TaskCompletionSource<Unit>();
            UndeferCommandsFor(kernel);
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
                _commandQueue.Enqueue(
                    new KernelOperation(
                        initCommand.command,
                        new TaskCompletionSource<KernelCommandResult>(),
                        initCommand.kernel));
            }
        }

        private void UndeferCommandsFor(Kernel kernel)
        {
            var commandsToKeepInDeferredList = new ConcurrentQueue<(KernelCommand command, Kernel kernel)>();
            while (_deferredCommands.TryDequeue(out var deferredCommand))
            {
                if (IsInPath(kernel, deferredCommand.kernel))
                {
                    _commandQueue.Enqueue(
                        new KernelOperation(
                            deferredCommand.command,
                            new TaskCompletionSource<KernelCommandResult>(),
                            deferredCommand.kernel));
                }
                else
                {
                    commandsToKeepInDeferredList.Enqueue(deferredCommand);
                }
            }

            while (commandsToKeepInDeferredList.TryDequeue(out var deferredCommand))
            {
                _deferredCommands.Enqueue(deferredCommand);
            }
           
            
            bool IsInPath(Kernel toTest, Kernel deferredCommandKernel)
            {
                while (toTest is not null)
                {
                    if (toTest == deferredCommandKernel)
                    {
                        return true;
                    }

                    toTest = toTest.ParentKernel;
                }
                return false;
            }
        }

        private static bool CanCancel(KernelCommand command)
        {
            return command switch
            {
                Quit _ => false,
                Cancel _ => false,
                _ => true
            };
        }

        public void CancelCommands()
        {
            foreach (var kernelInvocationContext in KernelInvocationContext.ActiveContexts.Where(c => !c.IsComplete && CanCancel(c.Command)))
            {
                kernelInvocationContext.Cancel();
            }

            using var disposables = new CompositeDisposable();
            var inFlightOperations = _commandQueue.Where(operation =>  CanCancel(operation.Command)).ToList();
            foreach (var inFlightOperation in inFlightOperations)
            {
                KernelInvocationContext currentContext = null;


                if (inFlightOperation is not null
                )
                {
                    currentContext = KernelInvocationContext.Establish(inFlightOperation.Command);
                    disposables.Add(currentContext.KernelEvents.Subscribe(inFlightOperation.Kernel.PublishEvent));
                    inFlightOperation.TaskCompletionSource.SetResult(currentContext.Result);
                }

                currentContext?.Cancel();
            }

            _deferredCommands.Clear();
            _commandQueue.Clear();
        }
    }
}