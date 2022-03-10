// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.ValueSharing;

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

                await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                    "vscode",
                    new Uri("kernel://vscode/vscode"),
                    new[] { "frontend" });

                var jsKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                                   "javascript",
                                   new Uri("kernel://webview/javascript"),
                                   new[] { "js" });

                jsKernel.UseValueSharing(new JavaScriptKernelValueDeclarer());
            }
        }
    }
}