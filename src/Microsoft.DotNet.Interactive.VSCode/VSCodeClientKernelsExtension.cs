// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
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

                var vscodeKernelName = new KernelName("vscode", new []{"frontend"});
                var vscode = await root.Host.DefaultConnector.ConnectKernelAsync(vscodeKernelName);

                var jsKernelName = new KernelName("javascript", new[] { "js" });
                var js =  await root.Host.DefaultConnector.ConnectKernelAsync(jsKernelName);

                root.Add(vscode, vscodeKernelName.Aliases);
                root.Add(js, jsKernelName.Aliases);
            }
            
        }
    }
}