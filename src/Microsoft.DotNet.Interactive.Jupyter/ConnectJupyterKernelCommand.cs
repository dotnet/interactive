// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Http;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class ConnectJupyterKernelCommand : ConnectKernelCommand
{
    public ConnectJupyterKernelCommand() : base("jupyter",
                                        "Connects to a installed jupyter kernel")
    {
        AddOption(KernelType);
        AddOption(TargetUrl);
        AddOption(Token);
        AddOption(UseBearerAuth);
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
        var targetUrl = commandLineContext.ParseResult.GetValueForOption(TargetUrl);
        var token = commandLineContext.ParseResult.GetValueForOption(Token);
        var useBearerAuth = commandLineContext.ParseResult.GetValueForOption(UseBearerAuth);

        JupyterKernelConnector connector = null;

        CompositeDisposable disposables = new CompositeDisposable();
        if (targetUrl is not null)
        {
            var connection = new JupyterHttpConnection(new Uri(targetUrl), token, useBearerAuth ? AuthType.Bearer : null);
            connector = new JupyterKernelConnector(connection, kernelType);
            disposables.Add(connection);
        }
        else
        {
            var connection = new LocalJupyterConnection();
            connector = new JupyterKernelConnector(connection, kernelType);
            disposables.Add(connection);
        }

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var kernel = await connector?.CreateKernelAsync(localName);
        kernel?.RegisterForDisposal(disposables);
        return kernel;
    }

}
