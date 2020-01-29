// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(
            DirectoryInfo directoryPath,
            string targetKernelName = null): base(targetKernelName)
        {
            Directory = directoryPath;
        }

        public DirectoryInfo Directory { get; }

        public override async Task InvokeAsync(KernelInvocationContext context)
        {
            await TryExtend(context.HandlingKernel);

            await context.HandlingKernel.VisitSubkernelsAsync(async k =>
            {
                await TryExtend(k);
            });

            async Task TryExtend(IKernel kernel)
            {
                if (kernel is IExtensibleKernel extensibleKernel)
                {
                    await extensibleKernel.LoadExtensionsFromDirectory(
                        Directory,
                        context);
                }
            }
        }
    }
}