// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class Quit : KernelCommand
    {
        public override Task InvokeAsync(KernelInvocationContext context)
        {
            try
            {
                return base.InvokeAsync(context);
            }
            catch (NoSuitableKernelException)
            {
                throw new InvalidOperationException(
                    $"The {nameof(Quit)} command has not been configured. In order to enable it, call {nameof(KernelExtensions)}.{nameof(KernelExtensions.UseQuitCommand)}");
            }
        }
    }
}