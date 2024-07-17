// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

public interface IJupyterKernelConnectionOptions
{
    IReadOnlyCollection<KernelDirectiveParameter> GetParameters();

    IJupyterConnection GetConnection(ConnectJupyterKernel connectCommand);
}