// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class ExplainCodeExtension
{
    public static Task LoadAsync(Kernel kernel)
    {
        var mermaidKernel = kernel
            .FindKernels(k => k.KernelInfo.LanguageName == "Mermaid")
            .FirstOrDefault();
        
        if (mermaidKernel is null)
        {
            throw new KernelException($"{nameof(ExplainCodeExtension)} requires a kernel that supports Mermaid language");
        }

        kernel.VisitSubkernelsAndSelf(k =>
        {
            if (k is CSharpKernel csharpKernel)
            {
                csharpKernel.AddDirective(new ExplainCSharpCode(mermaidKernel.KernelInfo));
                KernelInvocationContext.Current?.Display(
                    new HtmlString(@"<details><summary>ExplainCode</summary>
    <p>This extension generates Sequence diagrams from csharp code using Mermaid kernel.</p>
    </details>"),
                    "text/html");
            }
        });


        return Task.CompletedTask;
    }
}