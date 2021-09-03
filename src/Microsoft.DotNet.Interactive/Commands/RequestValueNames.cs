// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestValueNames : KernelCommand
    {
        public RequestValueNames(string targetKernelName) : base(targetKernelName)
        {
            
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            if (context.HandlingKernel is ISupportGetValue supportGetValuesKernel)
            {
                context.Publish(new ValueNamesProduced(supportGetValuesKernel.GetValueNames(), this));
                return Task.CompletedTask;
            }

            throw new InvalidOperationException($"Kernel {context.HandlingKernel.Name} doesn't support command {nameof(RequestValueNames)}");
        }
    
    }
}
