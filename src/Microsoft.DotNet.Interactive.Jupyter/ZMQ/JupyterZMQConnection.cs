using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using JupyterMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    internal class JupyterZMQConnection : IJupyterKernelConnection, IMessageSender, IMessageReceiver
    {
        private readonly RouterSocket _shell;
        private readonly PublisherSocket _ioPubSocket;
        private readonly string _shellAddress;
        private readonly string _ioPubAddress;
        private readonly CompositeDisposable _disposables;
        private readonly RequestReplyChannel _shellChannel;
        private readonly PubSubChannel _ioPubChannel;
        private readonly StdInChannel _stdInChannel;
        private readonly string _stdInAddress;
        private readonly string _controlAddress;
        private readonly RouterSocket _stdIn;
        private readonly RouterSocket _control;
        private readonly Subject<JupyterMessage> _subject;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _kernelIdentity = Guid.NewGuid().ToString();
        private readonly JupyterMessageSender _sender;

        public JupyterZMQConnection(ConnectionInformation connectionInformation)
        {
            if (connectionInformation is null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            _shellAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ShellPort}";
            _ioPubAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.IOPubPort}";
            _stdInAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.StdinPort}";
            _controlAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ControlPort}";

            var signatureAlgorithm = connectionInformation.SignatureScheme.Replace("-", string.Empty).ToUpperInvariant();
            var signatureValidator = new SignatureValidator(connectionInformation.Key, signatureAlgorithm);
            _shell = new RouterSocket();
            _ioPubSocket = new PublisherSocket();
            _stdIn = new RouterSocket();
            _control = new RouterSocket();

            _shellChannel = new RequestReplyChannel(new MessageSender(_shell, signatureValidator));
            _ioPubChannel = new PubSubChannel(new MessageSender(_ioPubSocket, signatureValidator));
            _stdInChannel = new StdInChannel(new MessageSender(_stdIn, signatureValidator), new MessageReceiver(_stdIn));

            _sender = new JupyterMessageSender(_ioPubChannel, _shellChannel, _stdInChannel, _kernelIdentity);
            _cancellationTokenSource = new CancellationTokenSource();
            _subject = new Subject<JupyterMessage>();

            _disposables = new CompositeDisposable
                           {
                               _shell,
                               _ioPubSocket,
                               _stdIn,
                               _control,
                               _cancellationTokenSource
                           };
        }

        public Uri TargetUri => throw new NotImplementedException();

        public IObservable<JupyterMessage> Messages => _subject;

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _disposables.Dispose();
        }

        public Task SendAsync(JupyterMessage message)
        {
            _shellChannel.Send(message);
            return Task.CompletedTask;
        }

        public Task StartAsync(string kernelType)
        {
            _shell.Bind(_shellAddress);
            _ioPubSocket.Bind(_ioPubAddress);
            _stdIn.Bind(_stdInAddress);
            _control.Bind(_controlAddress);

            StartListening(_shell, _cancellationTokenSource.Token);
            StartListening(_ioPubSocket, _cancellationTokenSource.Token);
            StartListening(_stdIn, _cancellationTokenSource.Token);
            StartListening(_control, _cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        private Task StartListening(NetMQSocket socket, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = socket.GetMessage();
                    _subject.OnNext(message);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }
    }
}

