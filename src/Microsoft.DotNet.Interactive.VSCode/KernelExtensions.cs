// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public static class KernelExtensions
    {
        public static Task UseVSCodeHelpersAsync<TKernel>(this TKernel kernel, Kernel rootKernel) where TKernel : DotNetKernel
        {
            var interactiveHost = new VSCodeInteractiveHost(rootKernel);
            return kernel.SetVariableAsync("InteractiveHost", interactiveHost, typeof(IInteractiveHost));
        }
    }
}
