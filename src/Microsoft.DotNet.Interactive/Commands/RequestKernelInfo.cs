// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestKernelInfo : KernelCommand
{
    public override async Task InvokeAsync(KernelInvocationContext context)
    {
        var kernel = context.HandlingKernel;

        context.Publish(new KernelInfoProduced(kernel.SupportedCommandTypes().Select(c => c.Name).ToArray(), context.Command as RequestKernelInfo));
    }
}