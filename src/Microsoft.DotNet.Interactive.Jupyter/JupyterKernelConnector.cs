// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class JupyterKernelConnector : IKernelConnector
{
    private readonly IJupyterConnection _jupyterConnection;
    private readonly string _kernelSpecName;

    public JupyterKernelConnector(IJupyterConnection jupyterConnection, string kernelSpecName)
    {
        _jupyterConnection = jupyterConnection;
        _kernelSpecName = kernelSpecName;
    }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernelConnection = await _jupyterConnection.CreateKernelConnectionAsync(_kernelSpecName);
        var kernel = await JupyterKernel.CreateAsync(kernelName, kernelConnection);

        var valueAdapterConfiguration = new CommValueAdapterConfiguration();
        await kernel.UseConfiguration(valueAdapterConfiguration);
        return kernel;
    }
}
