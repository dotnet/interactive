// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

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

internal class KernelSpecs
{
    public string @default {get; set;} 
    public Dictionary<string, KernelSpecDetail> kernelspecs { get; set; }
}

internal class KernelSpecDetail
{
    public string name { get; set; }
    public KernelSpec spec { get; set; }
    public object resources { get; set; }
}
#endregion

internal class JupyterHttpConnection : IJupyterConnection
{
    private readonly string _token;
    private readonly string _authType;
    private readonly HttpClient _httpClient;
    private readonly CompositeDisposable _disposables;
    private readonly Uri _serverUri;
    private string[] _availableKernels;

    public JupyterHttpConnection(Uri uri, string token, string authType = null)
    {
        _serverUri = uri;
        _token = token;
        _authType = authType ?? AuthType.Token;
        _httpClient = new HttpClient();
        _disposables = new CompositeDisposable
        {
            _httpClient
        };
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public async Task<string[]> ListAvailableKernelSpecsAsync()
    {
        if (_availableKernels == null)
        {
            HttpResponseMessage response = await SendWebRequestAsync(
                apiPath: "api/kernelspecs",
                body: null,
                contentType: "application/json",
                method: HttpMethod.Get
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ReasonPhrase);
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var list = JsonSerializer.Deserialize<KernelSpecs>(bytes);
            _availableKernels = list?.kernelspecs?.Keys.ToArray();
        }
        return _availableKernels;
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelType)
    {
        string kernelId = Guid.NewGuid().ToString();
        var body = new
        {
            kernel = new
            {
                id = kernelId,
                name = kernelType
            },
            name = "",
            path = $"dotnet-{kernelId}", 
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
            throw new KernelLaunchException(kernelType, response.ReasonPhrase);
        }

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        var session = JsonSerializer.Deserialize<KernelSessionInfo>(bytes);

        Uri socketUri = GetWebsocketUri($"/api/kernels/{session.kernel.id}/channels?token={_token}");
        return new JupyterKernelHttpConnection(socketUri, GetHttpUri($"api/kernels/{session.kernel.id}"));
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
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"{_authType} {_token}");
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
        return new Uri($"{_serverUri.AbsoluteUri}{apiPath}");
    }

    private Uri GetWebsocketUri(string apiPath)
    {
        string websocketScheme = _serverUri.Scheme == "http" ? "ws" : "wss";
        string socketUri = $"{websocketScheme}://{_serverUri.Authority}{apiPath}";

        return new Uri(socketUri);
    }

    
}
