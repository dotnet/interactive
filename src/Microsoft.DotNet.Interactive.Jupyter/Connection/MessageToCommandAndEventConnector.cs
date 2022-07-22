using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class MessageToCommandAndEventConnector : IKernelCommandAndEventSender, IKernelCommandAndEventReceiver, ICommandExecutionContext, IDisposable
    {
        private readonly Subject<CommandOrEvent> _commandOrEventsSubject;
        private readonly Uri _targetUri;
        private readonly CompositeDisposable _disposables;

        // handlers
        private readonly IKernelCommandToMessageHandler<SubmitCode> _submitCodeHandler;

        public MessageToCommandAndEventConnector(IMessageSender messageSender, IMessageReceiver messageReceiver, Uri targetUri)
        {
            _commandOrEventsSubject = new Subject<CommandOrEvent>();
            _targetUri = targetUri;

            _submitCodeHandler = new SubmitCodeHandler(messageSender, messageReceiver);

            _disposables = new CompositeDisposable
            {
                _commandOrEventsSubject
            };
        }

        public Uri RemoteHostUri => _targetUri;

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Publish(KernelEvent kernelEvent)
        {
            var commandOrEvent = new CommandOrEvent(kernelEvent);
            _commandOrEventsSubject.OnNext(commandOrEvent);
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            switch (kernelCommand)
            {
                case (SubmitCode submitCode):
                    await _submitCodeHandler.HandleCommandAsync(submitCode, this, cancellationToken);
                    break;
                default:
                    break;
            }
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<CommandOrEvent> observer)
        {
            return _commandOrEventsSubject.Subscribe(observer);
        }
    }
}
