// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal static class JupyterKernelExtensions
{
    public async static Task UseConfiguration(this JupyterKernel kernel, IJupyterKernelConfiguration configuration)
    {
        await configuration.ApplyAsync(kernel);
    }
}
