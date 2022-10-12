// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

internal class ZMQKernelConnection : IJupyterKernelConnection, IMessageSender, IMessageReceiver
{
    private readonly DealerSocket _shell;
    private readonly SubscriberSocket _ioSubSocket;
    private readonly string _shellAddress;
    private readonly string _ioSubAddress;
    private readonly CompositeDisposable _disposables;
    private readonly RequestReplyChannel _shellChannel;
    private readonly PubSubChannel _ioSubChannel;
    private readonly StdInChannel _stdInChannel;
    private readonly string _stdInAddress;
    private readonly string _controlAddress;
    private readonly DealerSocket _stdIn;
    private readonly DealerSocket _control;
    private readonly Subject<JupyterMessage> _subject;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ZMQKernelConnection(ConnectionInformation connectionInformation)
    {
        if (connectionInformation is null)
        {
            throw new ArgumentNullException(nameof(connectionInformation));
        }

        _shellAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ShellPort}";
        _ioSubAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.IOPubPort}";
        _stdInAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.StdinPort}";
        _controlAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ControlPort}";

        var signatureAlgorithm = connectionInformation.SignatureScheme.Replace("-", string.Empty).ToUpperInvariant();
        var signatureValidator = new SignatureValidator(connectionInformation.Key, signatureAlgorithm);
        _shell = new DealerSocket();
        _ioSubSocket = new SubscriberSocket();
        _stdIn = new DealerSocket();
        _control = new DealerSocket();

        _shellChannel = new RequestReplyChannel(new MessageSender(_shell, signatureValidator));
        _ioSubChannel = new PubSubChannel(new MessageSender(_ioSubSocket, signatureValidator));
        _stdInChannel = new StdInChannel(new MessageSender(_stdIn, signatureValidator), new MessageReceiver(_stdIn));

        _cancellationTokenSource = new CancellationTokenSource();
        _subject = new Subject<JupyterMessage>();

        _disposables = new CompositeDisposable
                       {
                           _shell,
                           _ioSubSocket,
                           _stdIn,
                           _control,
                           _cancellationTokenSource
                       };
    }

    public IObservable<JupyterMessage> Messages => _subject;

    public IMessageSender Sender => this;

    public IMessageReceiver Receiver => this;

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

    public Task StartAsync()
    {
        _shell.Connect(_shellAddress);
        _ioSubSocket.Connect(_ioSubAddress);
        _ioSubSocket.SubscribeToAnyTopic();
        _stdIn.Connect(_stdInAddress);
        _control.Connect(_controlAddress);

        StartListening(_ioSubSocket, _cancellationTokenSource.Token);
        StartListening(_shell, _cancellationTokenSource.Token);
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

