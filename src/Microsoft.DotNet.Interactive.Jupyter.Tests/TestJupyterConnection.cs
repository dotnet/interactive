// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Connection;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class TestJupyterConnection : IJupyterConnection, IDisposable
{
    private IJupyterConnection _testJupyterConnection;
    private bool _disposed = false;
    private readonly List<KernelSpec> _kernelSpecs = new();

    public TestJupyterConnection(
        TestJupyterKernelConnection testJupyterKernelConnection, 
        List<KernelSpec> kernelSpecs = null)
    {
        KernelConnection = testJupyterKernelConnection;
        if (kernelSpecs is not null)
        {
            _kernelSpecs = kernelSpecs;
        }
    }

    public void Attach(IJupyterConnection connection)
    {
        _testJupyterConnection = connection;
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelSpecName)
    {
        if (_testJupyterConnection is not null)
        {
            var kernelConnection = await _testJupyterConnection.CreateKernelConnectionAsync(kernelSpecName);
            KernelConnection.Attach(kernelConnection);
        }

        return KernelConnection;
    }

    public TestJupyterKernelConnection KernelConnection { get; }

    public void Dispose()
    {
        (_testJupyterConnection as IDisposable)?.Dispose();
        _disposed = true;
    }

    public Task<IEnumerable<KernelSpec>> GetKernelSpecsAsync()
    {
        if (_testJupyterConnection == null)
        {
            return Task.FromResult(_kernelSpecs.AsEnumerable());
        }

        return _testJupyterConnection.GetKernelSpecsAsync();
    }

    public bool IsDisposed => _disposed;
}