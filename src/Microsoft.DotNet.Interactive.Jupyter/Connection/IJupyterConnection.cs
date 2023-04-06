// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

public interface IJupyterConnection
{
    Task<IEnumerable<KernelSpec>> GetKernelSpecsAsync();

    Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelSpecName);
}
