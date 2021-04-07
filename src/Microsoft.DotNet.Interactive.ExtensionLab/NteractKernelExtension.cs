// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class NteractKernelExtension : IKernelExtension, IStaticContentSource
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            kernel.UseNteractDataExplorer();
            
            KernelInvocationContext.Current?.Display(
                new HtmlString($@"<details><summary>Explore data visually using the <a href=""https://github.com/nteract/data-explorer"">nteract Data Explorer</a>.</summary>
    <p>This extension adds the ability to sort, filter, and visualize data using the <a href=""https://github.com/nteract/data-explorer"">nteract Data Explorer</a>. Use the <code>ExploreWithNteract()</code> extension method with variables of type <code>JsonElement</code>, <code>IEnumerable<T></code> or <code>IDataView</code> to render the data explorer.</p>
<pre>
    <code>
    using Microsoft.Data.Analysis;
    using System.Collections.Generic;
    using Microsoft.ML;

    var dataFrame = DataFrame.LoadCsv(""./Data.csv"");

    dataFrame.ExploreWithNteract().Display();
    </code>
</pre>
    <img src=""https://user-images.githubusercontent.com/547415/109559345-621e5880-7a8f-11eb-8b98-d4feeaac116f.png"" width=""75%"">
    </details>"),
                "text/html");
            return Task.CompletedTask;
        }

        public string Name => "nteract";
    }
}