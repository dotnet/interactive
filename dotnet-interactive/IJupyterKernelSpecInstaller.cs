// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App
{
    public interface IJupyterKernelSpecInstaller
    {
        Task<bool> InstallKernel(DirectoryInfo kernelSpecPath, DirectoryInfo destination = null);
    }
}