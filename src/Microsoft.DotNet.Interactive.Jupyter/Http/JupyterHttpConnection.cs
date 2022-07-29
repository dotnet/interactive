using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Http
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

    internal class JupyterHttpConnection : IJupyterConnection
    {
        private readonly string _token;
        private readonly HttpClient _httpClient;
        private readonly CompositeDisposable _disposables;

        public JupyterHttpConnection(Uri targetUri, string token)
        {
            TargetUri = targetUri;
            _token = token;
            _httpClient = new HttpClient();
            _disposables = new CompositeDisposable
            {
                _httpClient
            };
        }

        public Uri TargetUri { get; }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelType)
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

            Uri socketUri = GetWebsocketUri($"/api/kernels/{session?.kernel?.id}/channels?token={_token}");
            return new JupyterKernelHttpConnection(socketUri);
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
                RequestUri = GetHttpUri(apiPath),
                Method = method
            };

            if (body is not null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await _httpClient.SendAsync(request);
        }

        private Uri GetHttpUri(string apiPath)
        {
            return new Uri($"{TargetUri.AbsoluteUri}{apiPath}");
        }

        private Uri GetWebsocketUri(string apiPath)
        {
            string websocketScheme = TargetUri.Scheme == "http" ? "ws" : "wss";
            string socketUri = $"{websocketScheme}://{TargetUri.Authority}{apiPath}";

            return new Uri(socketUri);
        }
    }
}
