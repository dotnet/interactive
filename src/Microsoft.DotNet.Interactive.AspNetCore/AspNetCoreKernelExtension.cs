// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.AspNetCore;

public class AspNetCoreKernelExtension
{
    public static Task LoadAsync(Kernel kernel)
    {
        kernel.VisitSubkernelsAndSelf(kernel =>
        {
            if (kernel is CSharpKernel cSharpKernel)
            {
                cSharpKernel.UseAspNetCore();
            }
        });

        return Task.CompletedTask;
    }
}