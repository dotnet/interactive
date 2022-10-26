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
    public ConnectJupyterKernelCommand() : base("jupyter",
                                        "Connects to a jupyter kernel")
    {
        AddOption(TargetUrl);
        AddOption(Token);
        AddOption(UseBearerAuth);
        AddOption(KernelType.AddCompletions(ctx => GetKernelSpecsCompletions(ctx)));
    }

    public Option<string> KernelType { get; } =
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

    public override async Task<Kernel> ConnectKernelAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var kernelType = commandLineContext.ParseResult.GetValueForOption(KernelType);

        var connection = GetJupyterConnection(commandLineContext.ParseResult);
        JupyterKernelConnector connector = new JupyterKernelConnector(connection, kernelType);
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
        var token = parseResult.GetValueForOption(Token);
        var useBearerAuth = parseResult.GetValueForOption(UseBearerAuth);

        if (targetUrl is not null)
        {
            var connection = new JupyterHttpConnection(new Uri(targetUrl), token, useBearerAuth ? AuthType.Bearer : null);
            return connection;
        }
        else
        {
            var connection = new LocalJupyterConnection();
            return connection;
        }
    }

    private List<CompletionItem> GetKernelSpecsCompletions(CompletionContext ctx)
    {
        var specCompletions = new List<CompletionItem>();
        using (var connection = GetJupyterConnection(ctx.ParseResult))
        {
            var specs = connection.ListAvailableKernelSpecsAsync().Result;
            if (specs != null)
            {
                foreach (var s in specs)
                {
                    specCompletions.Add(new CompletionItem(s));
                }
            }
        }
        return specCompletions;
    }
}
