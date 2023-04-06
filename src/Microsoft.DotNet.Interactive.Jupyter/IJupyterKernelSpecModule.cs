// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Jupyter;

public interface IJupyterKernelSpecModule
{
    Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory);
    DirectoryInfo GetDefaultKernelSpecDirectory();
    Task<IReadOnlyDictionary<string, KernelSpec>> ListKernels();
}