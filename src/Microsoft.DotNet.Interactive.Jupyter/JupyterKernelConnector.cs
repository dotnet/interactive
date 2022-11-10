// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class JupyterKernelConnector : IKernelConnector
{
    private readonly IJupyterConnection _jupyterConnection;
    private readonly string _kernelSpecName;

    public JupyterKernelConnector(IJupyterConnection jupyterConnection, string kernelSpecName)
    {
        _jupyterConnection = jupyterConnection ?? throw new ArgumentNullException(nameof(jupyterConnection));
        _kernelSpecName = kernelSpecName ?? throw new ArgumentNullException(nameof(kernelSpecName));
    }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernelConnection = await _jupyterConnection.CreateKernelConnectionAsync(_kernelSpecName);
        var commsManager = new CommsManager(kernelConnection.Sender, kernelConnection.Receiver);

        await kernelConnection.StartAsync();
        var kernel = await JupyterKernel.CreateAsync(kernelName, kernelConnection.Uri, kernelConnection.Sender, kernelConnection.Receiver);

        var valueAdapterConfiguration = new CommValueAdapterConfiguration(commsManager);
        await kernel.UseConfiguration(valueAdapterConfiguration);

        kernel.RegisterForDisposal(commsManager);
        kernel.RegisterForDisposal(kernelConnection);
        return kernel;
    }
}
