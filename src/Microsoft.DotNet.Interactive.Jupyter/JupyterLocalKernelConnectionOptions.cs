// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.Jupyter;

public sealed class JupyterLocalKernelConnectionOptions : IJupyterKernelConnectionOptions
{
    private static JupyterConnection _currentJupyterConnection;

    /// <summary>
    /// Represents connection to the kernels in the current environment
    /// </summary>
    private static JupyterConnection CurrentConnection
    {
        get
        {
            _currentJupyterConnection ??= new(new JupyterKernelSpecModule());
            return _currentJupyterConnection;
        }
    }

    public IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult)
    {
        return CurrentConnection;
    }

    public IReadOnlyCollection<Option> GetOptions()
    {
        return Array.Empty<Option>();
    }
}