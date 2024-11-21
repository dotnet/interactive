// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class ConnectJupyterKernelDirective : ConnectKernelDirective<ConnectJupyterKernel>
{
    private readonly List<IJupyterKernelConnectionOptions> _connectionCreators = new();

    public ConnectJupyterKernelDirective() : base("jupyter", "Connects a Jupyter kernel as a .NET Interactive subkernel.")
    {
        KernelSpecNameParameter.AddCompletions(GetKernelSpecNameCompletionsAsync);
        Parameters.Add(KernelSpecNameParameter);
        Parameters.Add(InitScriptParameter);
    }

    public KernelDirectiveParameter KernelSpecNameParameter { get; } =
        new("--kernel-spec", "The kernel spec to connect to")
        {
            Required = true
        };

    public KernelDirectiveParameter InitScriptParameter { get; } =
        new("--init-script", "Script to run on kernel initialization")
        {
            TypeHint = "file"
        };

    public ConnectJupyterKernelDirective AddConnectionOptions(IJupyterKernelConnectionOptions connectionOptions)
    {
        foreach (var option in connectionOptions.GetParameters())
        {
            Parameters.Add(option);
        }

        _connectionCreators.Add(connectionOptions);
        return this;
    }

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectJupyterKernel connectCommand,
        KernelInvocationContext context)
    {
        context.DisplayAs(
            "The `#!connect jupyter` feature is in preview. Please report any feedback or issues at https://github.com/dotnet/interactive/issues/new/choose.",
            "text/markdown");

        var kernelSpecName = connectCommand.KernelSpecName;
        var initScript = connectCommand.InitScript;

        var connection = GetJupyterConnection(connectCommand);
        if (connection is null)
        {
            throw new InvalidOperationException("No supported connection options were specified");
        }

        var connector = new JupyterKernelConnector(connection, kernelSpecName, initScript);

        var localName = connectCommand.ConnectedKernelName;

        var kernel = await connector.CreateKernelAsync(localName);
        if (connection is IDisposable disposableConnection)
        {
            kernel.RegisterForDisposal(disposableConnection);
        }
        return new[] { kernel };
    }

    private IJupyterConnection GetJupyterConnection(ConnectJupyterKernel connectCommand)
    {
        foreach (var connectionOptions in _connectionCreators)
        {
            var connection = connectionOptions.GetConnection(connectCommand);
            if (connection is not null)
            {
                return connection;
            }
        }
        return null;
    }

    private async Task GetKernelSpecNameCompletionsAsync(KernelDirectiveCompletionContext context)
    {
        var connection = GetJupyterConnection(new ConnectJupyterKernel(""));
        using var disposable = connection as IDisposable;
        var completions = new List<CompletionItem>();
        var specs = await connection.GetKernelSpecsAsync();
        if (specs is not null)
        {
            foreach (var spec in specs)
            {
                completions.Add(new CompletionItem(spec.Name, WellKnownTags.Parameter));
            }
        }

        foreach (var item in completions)
        {
            context.CompletionItems.Add(item);
        }
    }
}