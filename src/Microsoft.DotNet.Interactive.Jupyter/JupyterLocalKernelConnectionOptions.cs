// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.Jupyter;

public sealed class JupyterLocalKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private readonly IReadOnlyCollection<Option> _options;

    public Option<string> CondaEnv { get; } =
    new("--conda-env", "Conda environment to use; Default is base")
    {
    };

    public JupyterLocalKernelConnectionOptions()
    {
        _options = new List<Option>
        {
            CondaEnv.AddCompletions((ctx) => CondaEnvironment.GetEnvironments())
        };
    }

    public IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult)
    {
        var condaEnv = connectionOptionsParseResult.GetValueForOption(CondaEnv);
        IJupyterEnvironment environment = null;
        if (condaEnv != null)
        {
            environment = new CondaEnvironment(condaEnv);
        }

        return new JupyterConnection(new JupyterKernelSpecModule(environment));
    }

    public IReadOnlyCollection<Option> GetOptions()
    {
        return _options;
    }
}