// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MermaidKernelExtension : IKernelExtension, IStaticContentSource
    {
        public string Name => "Mermaid";
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                compositeKernel.Add(new MermaidKernel());
            }

            kernel.UseMermaid();
            KernelInvocationContext.Current?.Display(
                new HtmlString($@"<details><summary>Explain things visually using the <a href=""https://mermaid-js.github.io/mermaid/"">Mermaid language</a>.</summary>
    <p>TO DO</p>
<pre>
    <code>
    using Microsoft.Data.Analysis;
    using System.Collections.Generic;
    using Microsoft.ML;

    var dataFrame = DataFrame.LoadCsv(""./Data.csv"");

    dataFrame.ExploreWithSandDance()
    </code>
</pre>
    <img src=""https://mermaid-js.github.io/mermaid/img/header.png"" width=""30%"">
    </details>"),
                "text/html");

            return Task.CompletedTask;
        }

    }
}