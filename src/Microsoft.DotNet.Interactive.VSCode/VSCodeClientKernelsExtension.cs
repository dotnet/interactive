// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public class VSCodeClientKernelsExtension : IKernelExtension
    {
        public async Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel root)
            {
                root.UseVSCodeCommands();

                root.VisitSubkernels(k =>
                {
                    switch (k)
                    {
                        case CSharpKernel csharpKernel:
                            csharpKernel.UseVSCodeHelpers();
                            break;
                        case FSharpKernel fsharpKernel:
                            fsharpKernel.UseVSCodeHelpers();
                            break;
                        case PowerShellKernel powerShellKernel:
                            powerShellKernel.UseVSCodeHelpers();
                            break;
                    }
                });

                var vscodeKernelInfo = new KernelInfo("vscode", new []{"frontend"}, new Uri("kernel://vscode/vscode",UriKind.Absolute));

                var jsKernelInfo = new KernelInfo("javascript", new[] { "js", "vscode/js", "vscode/javascript" },new Uri("kernel://webview/javascript", UriKind.Absolute));

                await root.Host.CreateProxyKernelOnDefaultConnectorAsync(vscodeKernelInfo);
                await root.Host.CreateProxyKernelOnDefaultConnectorAsync(jsKernelInfo);
            }
            
        }
    }
}