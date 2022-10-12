// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JupyterMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

internal class JupyterKernelHttpConnection : IJupyterKernelConnection, IMessageSender, IMessageReceiver
{
    private readonly Uri _channelUri;
    private readonly ClientWebSocket _socket;
    private readonly Subject<JupyterMessage> _subject;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CompositeDisposable _disposables;

    public JupyterKernelHttpConnection(Uri channelUri)
    {
        _channelUri = channelUri;
        _socket = new ClientWebSocket();
        _subject = new Subject<JupyterMessage>();
        _cancellationTokenSource = new CancellationTokenSource();
        _disposables = new CompositeDisposable
        {
            _socket,
            _subject,
            _cancellationTokenSource
        };
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _disposables.Dispose();
    }

    public IObservable<JupyterMessage> Messages => _subject;

    public IMessageSender Sender => this;

    public IMessageReceiver Receiver => this;

    public async Task StartAsync()
    {
        await _socket.ConnectAsync(_channelUri, CancellationToken.None).ConfigureAwait(false);
        await StartListeningAsync(_socket, _cancellationTokenSource.Token);
    }

    public async Task SendAsync(JupyterMessage message)
    {
        var command = JsonSerializer.Serialize(message, MessageFormatter.SerializerOptions);
        var buffer = Encoding.UTF8.GetBytes(command);
        await SendToSocketAsync(buffer);
    }

    private async Task SendToSocketAsync(byte[] payload)
    {
        if (_socket.State != WebSocketState.Open)
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

    private Task StartListeningAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[512]);
                WebSocketReceiveResult result;

                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        do
                        {
                            result = await socket.ReceiveAsync(buffer, cancellationToken);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Close message received",
                            CancellationToken.None);
                            break;
                        }

                        ms.Seek(0, SeekOrigin.Begin);

                        var message = JsonSerializer.Deserialize<JupyterMessage>(ms, MessageFormatter.SerializerOptions);
                        _subject.OnNext(message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }
}
