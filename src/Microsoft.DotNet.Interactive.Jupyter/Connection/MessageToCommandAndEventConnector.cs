using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class MessageToCommandAndEventConnector : IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, ICommandExecutionContext, IDisposable
    {
        private readonly Subject<CommandOrEvent> _commandOrEventsSubject;
        private readonly Uri _targetUri;
        private readonly CompositeDisposable _disposables;
        
        private readonly ConcurrentDictionary<Type, Func<KernelCommand, ICommandExecutionContext, CancellationToken, Task>> _dynamicHandlers = new();
        private readonly KernelValueHandler _kernelValueHandler = new();
        

        public MessageToCommandAndEventConnector(IMessageSender messageSender, IMessageReceiver messageReceiver, Uri targetUri)
        {
            _commandOrEventsSubject = new Subject<CommandOrEvent>();
            _targetUri = targetUri;

            var submitCodeHandler = new SubmitCodeHandler(messageSender, messageReceiver);
            var requestKernelInfoHandler = new RequestKernelInfoHandler(messageSender, messageReceiver);
            var completionsHandler = new RequestCompletionsHandler(messageSender, messageReceiver);
            var hoverTipHandler = new RequestHoverTextHandler(messageSender, messageReceiver);

            RegisterCommandHandler<SubmitCode>(submitCodeHandler.HandleCommandAsync);
            RegisterCommandHandler<RequestKernelInfo>(requestKernelInfoHandler.HandleCommandAsync);
            RegisterCommandHandler<RequestCompletions>(completionsHandler.HandleCommandAsync);
            RegisterCommandHandler<RequestHoverText>(hoverTipHandler.HandleCommandAsync);

            // initialize request value handlers based on the language returned from the kernel
            var subscription = _commandOrEventsSubject.Subscribe(coe =>
            {
                if (coe.Event is KernelInfoProduced kip)
                {
                    ValueHandler = _kernelValueHandler.GetValueSupport(kip.KernelInfo.LanguageName, messageSender, messageReceiver);

                    if (ValueHandler is ISupportGetValue getValueHandler)
                    {
                        SupportGetValue(getValueHandler);
                    }
                }
            });

            _disposables = new CompositeDisposable
            {
                _commandOrEventsSubject,
                subscription
            };
        }

        public Uri RemoteHostUri => _targetUri;

        public IValueSupport ValueHandler { get; private set; }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void RegisterCommandHandler<TCommand>(Func<TCommand, ICommandExecutionContext, CancellationToken, Task> handler)
            where TCommand : KernelCommand
        {
            _dynamicHandlers[typeof(TCommand)] = (command, context, token) => handler((TCommand)command, context, token);
        }


        private Func<KernelCommand, ICommandExecutionContext, CancellationToken, Task> TryGetDynamicHandler(KernelCommand command)
        {
            if (_dynamicHandlers.TryGetValue(command.GetType(), out var handler))
            {
                return handler;
            }
            return null;
        }

        public void Publish(KernelEvent kernelEvent)
        {
            var commandOrEvent = new CommandOrEvent(kernelEvent);
            _commandOrEventsSubject.OnNext(commandOrEvent);
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            var handler = TryGetDynamicHandler(kernelCommand);
            if (handler != null)
            {
                await handler(kernelCommand, this, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                //TODO: trigger an explicit kernel interrupt as well to make sure the out-of-proc kernel 
                // stops any running executions.
            }
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            // TODO: could be used to translate events to jupyter message replies to the 
            // jupyter front end. 
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<CommandOrEvent> observer)
        {
            return _commandOrEventsSubject.Subscribe(observer);
        }

        private void SupportGetValue(ISupportGetValue languageValueHandler)
        {
            var valueHandler = new RequestValueHandler(languageValueHandler);
            RegisterCommandHandler<RequestValue>(valueHandler.HandleRequestValueAsync);
            RegisterCommandHandler<RequestValueInfos>(valueHandler.HandleRequestValueInfosAsync);
        }
    }
}
