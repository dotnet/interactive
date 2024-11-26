// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using JupyterMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Http.JupyterHttpConnection>;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

internal class JupyterKernelHttpConnection : IJupyterKernelConnection, IMessageSender, IMessageReceiver
{
    private readonly HttpApiClient _apiClient;
    private readonly IAuthorizationProvider _authProvider;
    private readonly ClientWebSocket _socket;
    private readonly Subject<JupyterMessage> _subject;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CompositeDisposable _disposables;
    private Task _messageLoopTask;

    public JupyterKernelHttpConnection(Uri serverUri, IAuthorizationProvider authProvider) :
        this(new HttpApiClient(serverUri, authProvider), authProvider)
    { }

    public JupyterKernelHttpConnection(HttpApiClient apiClient, IAuthorizationProvider authProvider)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        _socket = new ClientWebSocket();
        _subject = new Subject<JupyterMessage>();
        _cancellationTokenSource = new CancellationTokenSource();
        _disposables = new CompositeDisposable
        {
            Disposable.Create(() =>
            {
                if (_cancellationTokenSource.Token.CanBeCanceled)
                {
                    _cancellationTokenSource.Cancel();
                }
            }),
            _socket,
            _subject,
            Disposable.Create(() => _messageLoopTask?.Dispose()),
            _cancellationTokenSource
        };

        Uri = _apiClient.BaseUri;
    }

    public void Dispose() => _disposables.Dispose();

    public Uri Uri { get; }

    public IObservable<JupyterMessage> Messages => _subject;

    public IMessageSender Sender => this;

    public IMessageReceiver Receiver => this;

    public async Task StartAsync()
    {
        await ConnectSocketAsync();
        StartListening();
    }

    private async Task ConnectSocketAsync()
    {
        var token = await _authProvider.GetTokenAsync();
        var channelUri = GetWebSocketUri(_apiClient.GetUri($"/channels?token={token}"));
        await _socket.ConnectAsync(channelUri, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task SendAsync(JupyterMessage message)
    {
        if (await TryHandleAsync(message))
        {
            // handled here. no need to send to kernel
            return;
        }

        var command = JsonSerializer.Serialize(message, MessageFormatter.SerializerOptions);
        var buffer = Encoding.UTF8.GetBytes(command);
        await SendToSocketAsync(buffer);
    }

    private async Task SendToSocketAsync(byte[] payload)
    {
        if (_socket.State is not WebSocketState.Open)
        {
            string message = _socket.State switch
            {
                WebSocketState.Closed => string.IsNullOrEmpty(_socket.CloseStatusDescription)
                    ? $"Session closed: {_socket.CloseStatus}"
                    : $"Session closed. {_socket.CloseStatus} => {_socket.CloseStatusDescription}",
                WebSocketState.Aborted => "Session aborted!",
                _ => $"Session is about to close. State: {_socket.State}"
            };

            throw new IOException(message);
        }

        await _socket.SendAsync(
            payload.AsMemory(),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);
    }

    private void StartListening()
    {
        if (_messageLoopTask is not null)
        {
            throw new InvalidOperationException("Already listening.");
        }

        _messageLoopTask = Task.Factory.StartNew(
            MessageLoop,
            creationOptions: TaskCreationOptions.LongRunning);

        async Task MessageLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested &&
                   _socket.State is WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[512]);

                using MemoryStream ms = new MemoryStream();

                try
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType is WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Close message received",
                            CancellationToken.None);
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    var message = JsonSerializer.Deserialize<JupyterMessage>(ms, MessageFormatter.SerializerOptions);
                    PostMessage(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Log.Error(e, "Error in Jupyter message loop", e);
                }
            }
        }
    }

    private async Task<bool> TryHandleAsync(JupyterMessage message)
    {
        bool handled = false;
        if (message.Content is InterruptRequest)
        {
            handled = await InterruptKernelAsync();
            if (handled)
            {
                PostMessage(JupyterMessage.CreateReply(new InterruptReply(), message, message.Channel));
            }
        }

        return handled;
    }

    private async Task<bool> InterruptKernelAsync()
    {
        var response = await _apiClient.SendRequestAsync(
                           relativeApiPath: "interrupt",
                           content: null,
                           method: HttpMethod.Post);

        return response.IsSuccessStatusCode;
    }

    private Uri GetWebSocketUri(Uri uri)
    {
        var uriBuilder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme == "http" ? "ws" : "wss"
        };

        return uriBuilder.Uri;
    }

    private void PostMessage(JupyterMessage message)
    {
        _subject.OnNext(message);
    }
}
