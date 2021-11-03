// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SandDanceKernelExtension : IKernelExtension, IStaticContentSource
    {
        public string Name => "SandDance";
        public Task OnLoadAsync(Kernel kernel)
        {
            kernel.UseSandDanceExplorer(libraryUri:new Uri(@"https://colombod.github.io/dotnet-interactive-cdn/extensionlab/1.0.252001/SandDance/sanddanceapi.js", UriKind.Absolute), libraryVersion: "1.0.252001");

            KernelInvocationContext.Current?.Display(
                new HtmlString($@"<details><summary>Explore data visually using the <a href=""https://github.com/microsoft/SandDance"">SandDance Explorer</a>.</summary>
    <p>This extension adds the ability to sort, filter, and visualize data using the <a href=""https://github.com/microsoft/SandDance"">SandDance Explorer</a>. Use the <code>ExploreWithSandDance()</code> extension method with variables of type <code>JsonElement</code>, <code>IEnumerable<T></code> or <code>IDataView</code> to render the data explorer.</p>
<pre>
    <code>
    using Microsoft.Data.Analysis;
    using System.Collections.Generic;
    using Microsoft.ML;

    var dataFrame = DataFrame.LoadCsv(""./Data.csv"");

    dataFrame.ExploreWithSandDance().Display();
    </code>
</pre>
<p>To set the SandDance Explorer use the following code</p>
<pre>
    <code>
    DataExplorer.SetDefault<TabularDataResource, SandDanceDataExplorer>();
    </code>
</pre>
    <img src=""https://user-images.githubusercontent.com/11507384/54236654-52d42800-44d1-11e9-859e-6c5d297a46d2.gif"" width=""30%"">
    </details>"),
                "text/html");

            return Task.CompletedTask;
        }
    }
}