// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App
{
    public interface IJupyterKernelSpec
    {
        Task<CommandLineResult> InstallKernel(DirectoryInfo directory);

        Task<CommandLineResult> UninstallKernel(DirectoryInfo directory);
        bool CanInstall { get;}
    }
}