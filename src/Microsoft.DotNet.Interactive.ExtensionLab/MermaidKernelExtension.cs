// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
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

            kernel.UseMermaid();
            
            var message = new HtmlString(
                $@"<details><summary>Explain things visually using the <a href=""https://mermaid-js.github.io/mermaid/"">Mermaid language</a>.</summary>
    <p>This extension adds a new kernel that can render mermaid markdown. This code will render a diagram</p>
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
    Note right of John: Rational thoughts <br/>prevail!
    John-->>Alice: Great!
    John->>Bob: How about you?
    Bob-->>John: Jolly good!
    </code>
</pre>
It also add gestures to render a class diagram from any type or instance. Use the <code>Explain()</code> extension method on object to render its class diagram. You can also control the graph depth passing a value to the extension method.

<pre>
    <code>
using Microsoft.DotNet.Interactive;

Kernel.Root.Explain(graphDepth: 2);
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