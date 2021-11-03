// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MermaidKernelExtension : IKernelExtension, IStaticContentSource
    {
        public string Name => "Mermaid";
        public async Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                compositeKernel.Add(new MermaidKernel());
            }

            kernel.UseMermaid(libraryUri: new Uri(@"https://colombod.github.io/dotnet-interactive-cdn/extensionlab/1.0.252001/mermaid/mermaidapi.js", UriKind.Absolute), libraryVersion: "1.0.252001");
            
            var message = new HtmlString(
                $@"<details><summary>Explain things visually using the <a href=""https://mermaid-js.github.io/mermaid/"">Mermaid language</a>.</summary>
    <p>This extension adds a new kernel that can render Mermaid markdown. This code will render a sequence diagram:</p>
<pre>
    <code>
#!mermaid
sequenceDiagram
    participant Alice
    participant Bob
    Alice->>John: Hello John, how are you?
    loop Healthcheck
        John->>John: Fight against hypochondria
    end
    Note right of John: Rational thoughts prevail!
    John-->>Alice: Great!
    John->>Bob: How about you?
    Bob-->>John: Jolly good!
    </code>
</pre>
<p>This extension also adds gestures to render a class diagram from any type. Use the <code>ExploreWithUmlClassDiagram().Display();</code> extension method on <code>System.Type</code> to render its class diagram.</p>

<pre>
    <code>
typeof(List&lt;string&gt;).ExploreWithUmlClassDiagram().Display();
    </code>
</pre>
    <img src=""https://mermaid-js.github.io/mermaid/img/header.png"" width=""30%"">
    </details>");

           
            var formattedValue = new FormattedValue(
                HtmlFormatter.MimeType,
                message.ToDisplayString(HtmlFormatter.MimeType));
            
            await kernel.SendAsync(new DisplayValue(formattedValue, Guid.NewGuid().ToString()));

        }
    }
}