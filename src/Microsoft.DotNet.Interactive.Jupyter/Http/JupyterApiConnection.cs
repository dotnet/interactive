using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.IO.Pipelines;
using System.Buffers;
using System.IO;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using JupyterMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Formatting;
using System.Reactive.Threading.Tasks;
using System.Collections.Concurrent;
using System.Speech.Synthesis;

namespace Microsoft.DotNet.Interactive.Jupyter.KernelProxy
{
    #region JsonTypes
    internal class KernelSessionInfo
    {
        public string id { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public KernelInfo kernel { get; set; }
    }

    internal class KernelInfo
    {
        public string id { get; set; }
        public string name { get; set; }

        public string last_activity { get; set; }
        public string execution_state { get; set; }
        public int connections { get; set; }
    }


    #endregion
    internal class JupyterApiConnection : IJupyterKernelConnection, IMessageSender, IMessageReceiver
    {
        private readonly string _token;
        private readonly HttpClient _httpClient;
        private readonly ClientWebSocket _socket;
        private readonly Subject<JupyterMessage> _subject;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public JupyterApiConnection(Uri targetUri, string token)
        {
            TargetUri = targetUri;
            _token = token;
            _httpClient = new HttpClient();
            _socket = new ClientWebSocket();
            _subject = new Subject<JupyterMessage>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Uri TargetUri { get; }

        public IObservable<JupyterMessage> Messages => _subject;

        public void Dispose()
        {
            _socket.Dispose();
            _httpClient.Dispose();
            _subject.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public async Task StartAsync(string kernelType)
        {
            var body = new
            {
                kernel = new
                {
                    id = 123,
                    name = kernelType
                },
                name = "",
                path = $"dotnet-{Guid.NewGuid().ToString()}", // TODO: Find out how to get the filename
                type = "notebook"
            };

            HttpResponseMessage response = await SendWebRequestAsync(
                apiPath: "api/sessions",
                body: JsonSerializer.Serialize(body),
                contentType: "application/json",
                method: HttpMethod.Post
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Kernel Launch failed");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var session = JsonSerializer.Deserialize<KernelSessionInfo>(bytes);

            string websocketScheme = TargetUri.Scheme == "http" ? "ws" : "wss";
            string socketUri = $"{websocketScheme}://{TargetUri.Authority}/api/kernels/{session?.kernel?.id}/channels?token={_token}";
            await _socket.ConnectAsync(new Uri(socketUri), CancellationToken.None).ConfigureAwait(false);

            await StartListeningAsync(_socket, _cancellationTokenSource.Token);

            var request = JupyterMessage.Create(new KernelInfoRequest());
            var reply = Messages.ChildOf(request)
                                 .SelectContent()
                                 .TakeUntilMessage<KernelInfoReply>()
                                 .ToTask();
            await SendAsync(request).ConfigureAwait(false);
            await reply.ConfigureAwait(false); // wait for kernel reply
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

        private async Task<HttpResponseMessage> SendWebRequestAsync(
            string apiPath,
            string body,
            string contentType,
            HttpMethod method)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(".NET internative");

            if (_token is not null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_token}");
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{TargetUri.AbsoluteUri}{apiPath}"),
                Method = method
            };

            if (body is not null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await _httpClient.SendAsync(request);
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
}
