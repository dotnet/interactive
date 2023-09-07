// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

internal class JupyterTokenProvider : IAuthorizationProvider
{
    private readonly string _token;
    private readonly string _authScheme;

    public JupyterTokenProvider(string token, string authScheme = null)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
        _authScheme = authScheme ?? AuthorizationScheme.Token;
    }

    public string AuthScheme => _authScheme;

    public Task<string> GetTokenAsync()
    {
        return Task.FromResult(_token);
    }

    public string GetToken()
    {
        return _token;
    }
}
