// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

internal class JupyterHttpConnection : IJupyterConnection, IDisposable
{
    #region JsonTypes
    private class KernelSessionInfo
    {
        public string id { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public KernelInfo kernel { get; set; }
    }

    private class KernelInfo
    {
        public string id { get; set; }
        public string name { get; set; }

        public string last_activity { get; set; }
        public string execution_state { get; set; }
        public int connections { get; set; }
    }

    private class KernelSpecs
    {
        public string @default { get; set; }
        public Dictionary<string, KernelSpecDetail> kernelspecs { get; set; }
    }

    private class KernelSpecDetail
    {
        public string name { get; set; }
        public KernelSpec spec { get; set; }
        public object resources { get; set; }
    }
    #endregion

    private readonly HttpApiClient _apiClient;
    private readonly IAuthorizationProvider _authProvider;
    private readonly CompositeDisposable _disposables;
    private IEnumerable<KernelSpec> _availableKernels;
    private List<string> _activeSessions = new();

    public JupyterHttpConnection(Uri serverUri, IAuthorizationProvider authProvider) :
        this(new HttpApiClient(serverUri, authProvider), authProvider)
    { }

    public JupyterHttpConnection(HttpApiClient apiClient, IAuthorizationProvider authProvider)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));

        _disposables = new CompositeDisposable
        {
            _apiClient
        };
    }

    public void Dispose()
    {
        ShutdownJupyterSessions();
        _disposables.Dispose();
    }

    public async Task<IEnumerable<KernelSpec>> GetKernelSpecsAsync()
    {
        if (_availableKernels == null)
        {
            HttpResponseMessage response = await _apiClient.SendRequestAsync(
                relativeApiPath: "api/kernelspecs",
                content: null,
                method: HttpMethod.Get
            );

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<KernelSpec>();
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var list = JsonSerializer.Deserialize<KernelSpecs>(bytes);
            _availableKernels = list?.kernelspecs?.Select(t =>
            {
                var spec = t.Value.spec;
                spec.Name ??= t.Key;
                return spec;
            });
        }

        return _availableKernels;
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelSpecName)
    {
        string kernelId = Guid.NewGuid().ToString();
        var body = new
        {
            kernel = new
            {
                id = kernelId,
                name = kernelSpecName
            },
            name = "",
            path = $"dotnet-{kernelId}",
            type = "notebook"
        };

        HttpResponseMessage response = await _apiClient.SendRequestAsync(
            relativeApiPath: "api/sessions",
            content: new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            method: HttpMethod.Post
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new KernelStartException(kernelSpecName, response.ReasonPhrase);
        }

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        var session = JsonSerializer.Deserialize<KernelSessionInfo>(bytes);
        _activeSessions.Add(session.id);
        var kernelApiClient = _apiClient.CreateClient($"api/kernels/{session.kernel.id}");

        return new JupyterKernelHttpConnection(kernelApiClient, _authProvider);
    }

    private bool ShutdownJupyterSessions()
    {
        bool success = true;
        foreach (var session in _activeSessions)
        {
            HttpResponseMessage response = _apiClient.SendRequest(
                    relativeApiPath: $"api/sessions/{session}",
                    content: null,
                    method: HttpMethod.Delete
                );


            success &= response.IsSuccessStatusCode;
        }

        return success;
    }
}
