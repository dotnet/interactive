// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Http;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class ConnectJupyterKernelCommand : ConnectKernelCommand
{
    private readonly List<IJupyterKernelConnectionOptions> _connectionCreaters = new();
    private KeyValuePair<int, IEnumerable<CompletionItem>> _mruKernelSpecSuggestions;

    public ConnectJupyterKernelCommand() : base("jupyter",
                                        "Connects to a jupyter kernel")
    {
        AddOption(InitScript);
        AddOption(KernelSpecName.AddCompletions(ctx => GetKernelSpecsCompletions(ctx)));
    }

    public Option<string> KernelSpecName { get; } =
    new("--kernel-spec", "The kernel spec to connect to")
    {
        IsRequired = true
    };


    public Option<string> InitScript { get; } =
    new("--init-script", "Script to run on kernel initialization")
    {
    };

    public ConnectJupyterKernelCommand AddConnectionOptions(IJupyterKernelConnectionOptions connectionOptions)
    {
        foreach (var option in connectionOptions.GetOptions())
        {
            AddOption(option);
        }

        _connectionCreaters.Add(connectionOptions);
        return this;
    }

    public override async Task<Kernel> ConnectKernelAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var kernelSpecName = commandLineContext.ParseResult.GetValueForOption(KernelSpecName);
        var initScript = commandLineContext.ParseResult.GetValueForOption(InitScript);

        var connection = GetJupyterConnection(commandLineContext.ParseResult);
        if (connection == null)
        {
            throw new InvalidOperationException("No supported connection options were specified");
        }
        
        JupyterKernelConnector connector = new JupyterKernelConnector(connection, kernelSpecName, initScript);

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var kernel = await connector?.CreateKernelAsync(localName);
        kernel?.RegisterForDisposal(connection);
        return kernel;
    }

    private IJupyterConnection GetJupyterConnection(ParseResult parseResult)
    {
        foreach (var connectionOptions in _connectionCreaters)
        {
            var connection = connectionOptions.GetConnection(parseResult);
            if (connection != null)
            {
                return connection;
            }
        }
        return null;
    }

    private IEnumerable<CompletionItem> GetKernelSpecsCompletions(CompletionContext ctx)
    {
        var hash = GetParseResultHash(ctx.ParseResult);
        if (_mruKernelSpecSuggestions.Key == hash)
        {
            return _mruKernelSpecSuggestions.Value;
        }

        IEnumerable<CompletionItem> completions;
        using (var connection = GetJupyterConnection(ctx.ParseResult))
        {
            completions = GetKernelSpecsCompletions(connection);
        }

        if (completions is not null)
        {
            _mruKernelSpecSuggestions = new(hash, completions);
        }
        return completions;
    }

    private IEnumerable<CompletionItem> GetKernelSpecsCompletions(IJupyterConnection connection)
    {
        var completions = new List<CompletionItem>();
        var specs = connection.GetKernelSpecsAsync().GetAwaiter().GetResult();
        if (specs != null)
        {
            foreach (var s in specs)
            {
                completions.Add(new CompletionItem(s.Name));
            }
        }

        return completions;
    }

    private int GetParseResultHash(ParseResult parseResult)
    {
        string values = string.Empty;
        foreach (var option in Options)
        {
            if (option != KernelSpecName)
            {
                values += parseResult.GetValueForOption(option);
            }
        }

        return (values).GetHashCode();
    }
}

public sealed class JupyterLocalKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private readonly IJupyterConnection _defaultConnection = JupyterConnection.CurrentEnvironment;

    public IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult)
    {
        return _defaultConnection;
    }

    public IReadOnlyCollection<Option> GetOptions()
    {
        return Array.Empty<Option>();
    }
}

public sealed class JupyterHttpKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private readonly IReadOnlyCollection<Option> _options;

    public Option<string> TargetUrl { get; } =
    new("--url", "URl to connect to the jupyter server")
    {
    };

    public Option<string> Token { get; } =
    new("--token", "token to connect to the jupyter server")
    {
    };

    private Option<bool> UseBearerAuth { get; } =
    new("--bearer", "auth type is bearer token")
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