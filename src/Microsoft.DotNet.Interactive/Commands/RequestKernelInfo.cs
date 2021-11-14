// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestKernelInfo : KernelCommand
{
    public override Task InvokeAsync(KernelInvocationContext context)
    {
        var kernel = context.HandlingKernel;

        var host = kernel switch
        {
            CompositeKernel c => c.Host,
            _ => kernel.ParentKernel.Host
        };

        if (host is { })
        {
            if (host.TryGetKernelInfo(kernel, out var kernelInfo))
            {
                context.Publish(new KernelInfoProduced(kernelInfo, context.Command as RequestKernelInfo));
            }
        }
    }
}