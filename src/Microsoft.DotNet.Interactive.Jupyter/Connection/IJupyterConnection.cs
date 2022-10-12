// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

public interface IJupyterConnection : IDisposable
{
    Uri TargetUri { get; }

    Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelSpec);
}
