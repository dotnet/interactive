// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class ConnectJupyterKernelCommand : ConnectKernelCommand
{
    private readonly List<IJupyterKernelConnectionOptions> _connectionCreators = new();
    private KeyValuePair<int, IEnumerable<CompletionItem>> _mruKernelSpecSuggestions;

    public ConnectJupyterKernelCommand() : base("jupyter", "Connects a Jupyter kernel as a .NET Interactive subkernel.")
    {
        AddOption(KernelSpecName.AddCompletions(GetKernelSpecsCompletions));
        AddOption(InitScript);
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

        _connectionCreators.Add(connectionOptions);
        return this;
    }

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        context.DisplayAs(
            "The `#!connect jupyter` feature is in preview. Please report any feedback or issues at https://github.com/dotnet/interactive/issues/new/choose.", 
            "text/markdown");

        var kernelSpecName = commandLineContext.ParseResult.GetValueForOption(KernelSpecName);
        var initScript = commandLineContext.ParseResult.GetValueForOption(InitScript);

        var connection = GetJupyterConnection(commandLineContext.ParseResult);
        if (connection is null)
        {
            throw new InvalidOperationException("No supported connection options were specified");
        }

        var connector = new JupyterKernelConnector(connection, kernelSpecName, initScript);

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var kernel = await connector.CreateKernelAsync(localName);
        if (connection is IDisposable disposableConnection)
        {
            kernel.RegisterForDisposal(disposableConnection);
        }
        return new[]{kernel};
    }

    private IJupyterConnection GetJupyterConnection(ParseResult parseResult)
    {
        foreach (var connectionOptions in _connectionCreators)
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
        var connection = GetJupyterConnection(ctx.ParseResult);
        using (connection as IDisposable) {
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