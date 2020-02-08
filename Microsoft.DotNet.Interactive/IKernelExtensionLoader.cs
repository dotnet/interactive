// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive
{
    public interface IKernelExtensionLoader 
    {
        Task LoadFromDirectoryAsync(
            DirectoryInfo directory,
            IExtensibleKernel kernel,
            KernelInvocationContext context);
    }
}