// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.PowerShell;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class JupyterClientKernelExtension : IKernelExtension
{
    public Task OnLoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel root)
        {
            root.Add(
                new JavaScriptKernel(),
                new[] { "js" });

            root.VisitSubkernels(k =>
            {
                switch (k)
                {
                    case CSharpKernel csharpKernel:
                        csharpKernel.UseJupyterHelpers();
                        break;

                    case PowerShellKernel powerShellKernel:
                        powerShellKernel.UseJupyterHelpers();
                        break;
                }
            });
        }

        return Task.CompletedTask;
    }
}