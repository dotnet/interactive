// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.CommandEvents;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal class JupyterKernelConnector
{
    private readonly IJupyterConnection _jupyterConnection;
    private readonly string _kernelSpecName;
    private readonly string _initScript;

    public JupyterKernelConnector(IJupyterConnection jupyterConnection, string kernelSpecName, string initScript)
    {
        _jupyterConnection = jupyterConnection ?? throw new ArgumentNullException(nameof(jupyterConnection));
        _kernelSpecName = kernelSpecName ?? throw new ArgumentNullException(nameof(kernelSpecName));
        _initScript = initScript;
    }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernelConnection = await _jupyterConnection.CreateKernelConnectionAsync(_kernelSpecName);
        var commsManager = new CommsManager(kernelConnection.Sender, kernelConnection.Receiver);

        await kernelConnection.StartAsync();
        var kernel = await JupyterKernel.CreateAsync(kernelName, kernelConnection.Sender, kernelConnection.Receiver);

        if (!string.IsNullOrEmpty(_initScript))
        {
            await kernel.RunOnKernelAsync(_initScript);
        }

        var configuration = new CommCommandEventChannelConfiguration(commsManager);
        await kernel.UseConfiguration(configuration);

        kernel.RegisterForDisposal(commsManager);
        kernel.RegisterForDisposal(kernelConnection);
        return kernel;
    }
}
