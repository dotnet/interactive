// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Http;

namespace Microsoft.DotNet.Interactive.Jupyter;

public sealed class JupyterHttpKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private readonly IReadOnlyCollection<KernelDirectiveParameter> _parameters;

    public KernelDirectiveParameter TargetUrl { get; } =
        new("--url", "URL to connect to a remote jupyter server");

    public KernelDirectiveParameter Token { get; } =
        new("--token", "token to connect to a remote jupyter server");

    private KernelDirectiveParameter UseBearerAuth { get; } =
        new("--bearer", "auth type is bearer token for remote jupyter server");

    public JupyterHttpKernelConnectionOptions()
    {
        _parameters = new List<KernelDirectiveParameter>
        {
            TargetUrl,
            Token,
            UseBearerAuth
        };
    }

    public IJupyterConnection GetConnection(ConnectJupyterKernel connectCommand)
    {
        var targetUrl = connectCommand.TargetUrl;

        if (targetUrl is null)
        {
            return null;
        }

        var token = connectCommand.Token;
        var useBearerAuth = connectCommand.UseBearerAuth;

        var connection = new JupyterHttpConnection(
            new Uri(targetUrl),
            new JupyterTokenProvider(token,
                                     useBearerAuth
                                         ? AuthorizationScheme.Bearer
                                         : null));
        return connection;
    }

    public IReadOnlyCollection<KernelDirectiveParameter> GetParameters()
    {
        return _parameters;
    }
}