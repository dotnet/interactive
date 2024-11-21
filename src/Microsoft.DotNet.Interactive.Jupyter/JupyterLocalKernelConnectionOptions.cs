// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;

namespace Microsoft.DotNet.Interactive.Jupyter;

public sealed class JupyterLocalKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private readonly IReadOnlyCollection<KernelDirectiveParameter> _parameters;

    public JupyterLocalKernelConnectionOptions()
    {
        CondaEnv.AddCompletions(async context =>
        {
            foreach (var name in await CondaEnvironment.GetEnvironmentNamesAsync())
            {
                context.CompletionItems.Add(new CompletionItem(name, WellKnownTags.Parameter));
            }
        });

        _parameters = new List<KernelDirectiveParameter>
        {
            CondaEnv
        };
    }

    public KernelDirectiveParameter CondaEnv { get; } = new("--conda-env", "The Conda environment to use. (The default is base.)");

    public IJupyterConnection GetConnection(ConnectJupyterKernel connectCommand)
    {
        var condaEnv = connectCommand.CondaEnv;
        IJupyterEnvironment environment = null;
        if (condaEnv is not null)
        {
            environment = new CondaEnvironment(condaEnv);
        }

        return new JupyterConnection(new JupyterKernelSpecModule(environment));
    }

    public IReadOnlyCollection<KernelDirectiveParameter> GetParameters()
    {
        return _parameters;
    }
}