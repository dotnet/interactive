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
    private readonly IJupyterConnection _localConnection = JupyterConnection.Local;
    private KeyValuePair<int, IEnumerable<CompletionItem>> _mruCompletionList;

    public ConnectJupyterKernelCommand() : base("jupyter",
                                        "Connects to a jupyter kernel")
    {
        AddOption(TargetUrl);
        AddOption(Token);
        AddOption(UseBearerAuth);
        AddOption(InitScript);
        AddOption(KernelSpecName.AddCompletions(ctx => GetKernelSpecsCompletions(ctx)));
    }

    public Option<string> KernelSpecName { get; } =
    new("--kernel-spec", "The kernel spec to connect to")
    {
        IsRequired = true
    };

    public Option<string> TargetUrl { get; } =
    new("--url", "URl to connect to the jupyter server")
    {
    };

    public Option<string> Token { get; } =
    new("--token", "token to connect to the jupyter server")
    {
    };

    public Option<bool> UseBearerAuth { get; } =
    new("--bearer", "auth type is bearer token")
    {
    };

    public Option<string> InitScript { get; } =
    new("--init-script", "Script to run on kernel initialization")
    {
    };

    public override async Task<Kernel> ConnectKernelAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var kernelSpecName = commandLineContext.ParseResult.GetValueForOption(KernelSpecName);
        var initScript = commandLineContext.ParseResult.GetValueForOption(InitScript);

        var connection = GetJupyterConnection(commandLineContext.ParseResult) ?? _localConnection;
        JupyterKernelConnector connector = new JupyterKernelConnector(connection, kernelSpecName, initScript);

        CompositeDisposable disposables = new CompositeDisposable
        {
            connection
        };

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var kernel = await connector?.CreateKernelAsync(localName);
        kernel?.RegisterForDisposal(disposables);
        return kernel;
    }

    private IJupyterConnection GetJupyterConnection(ParseResult parseResult)
    {
        var targetUrl = parseResult.GetValueForOption(TargetUrl);

        if (targetUrl is null)
        {
            return null;
        }

        var token = parseResult.GetValueForOption(Token);
        var useBearerAuth = parseResult.GetValueForOption(UseBearerAuth);

        var connection = new JupyterHttpConnection(new Uri(targetUrl), new JupyterTokenProvider(token, useBearerAuth ? AuthorizationScheme.Bearer : null));
        return connection;
    }

    private IEnumerable<CompletionItem> GetKernelSpecsCompletions(CompletionContext ctx)
    {
        var hash = GetOptionHash(ctx.ParseResult);
        if (_mruCompletionList.Key == hash)
        {
            return _mruCompletionList.Value;
        }

        IEnumerable<CompletionItem> completions;
        using (var connection = GetJupyterConnection(ctx.ParseResult))
        {
            completions = GetKernelSpecsCompletions(connection ?? _localConnection);
        }

        if (completions is not null)
        {
            _mruCompletionList = new(hash, completions);
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

    private int GetOptionHash(ParseResult parseResult)
    {
        var targetUrl = parseResult.GetValueForOption(TargetUrl);
        var token = parseResult.GetValueForOption(Token);

        return (targetUrl + token).GetHashCode();
    }
}
