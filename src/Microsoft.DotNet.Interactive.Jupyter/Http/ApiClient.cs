using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

internal class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly Uri _baseUri;
    private readonly IAuthorizationProvider _authProvider;
    
    public ApiClient(Uri baseUri, IAuthorizationProvider authProvider)
    {
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
    }

    public Uri BaseUri => _baseUri;

    public virtual ApiClient CreateClient(string relativeApiPath)
    {
        return new ApiClient(new Uri(_baseUri, relativeApiPath), _authProvider);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    public virtual async Task<HttpResponseMessage> SendRequestAsync(string relativeApiPath, HttpContent content, HttpMethod method)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(".NET internative");

        var token = await _authProvider.GetTokenAsync();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"{_authProvider.AuthScheme} {token}");

        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(_baseUri, relativeApiPath),
            Method = method, 
            Content = content
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return await _httpClient.SendAsync(request);
    }
}
