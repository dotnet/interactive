// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Http;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.Jupyter;

public sealed class JupyterHttpKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private readonly IReadOnlyCollection<Option> _options;

    public Option<string> TargetUrl { get; } =
    new("--url", "URL to connect to a remote jupyter server")
    {
    };

    public Option<string> Token { get; } =
    new("--token", "token to connect to a remote jupyter server")
    {
    };

    private Option<bool> UseBearerAuth { get; } =
    new("--bearer", "auth type is bearer token for remote jupyter server")
    {
    };

    public JupyterHttpKernelConnectionOptions()
    {
        _options = new List<Option>
        {
            TargetUrl,
            Token,
            UseBearerAuth
        };
    }

    public IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult)
    {
        var targetUrl = connectionOptionsParseResult.GetValueForOption(TargetUrl);

        if (targetUrl is null)
        {
            return null;
        }

        var token = connectionOptionsParseResult.GetValueForOption(Token);
        var useBearerAuth = connectionOptionsParseResult.GetValueForOption(UseBearerAuth);

        var connection = new JupyterHttpConnection(new Uri(targetUrl), new JupyterTokenProvider(token, useBearerAuth ? AuthorizationScheme.Bearer : null));
        return connection;
    }

    public IReadOnlyCollection<Option> GetOptions()
    {
        return _options;
    }
}