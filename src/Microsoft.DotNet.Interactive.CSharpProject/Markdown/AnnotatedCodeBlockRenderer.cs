// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

public class AnnotatedCodeBlockRenderer : CodeBlockRenderer
{
    public AnnotatedCodeBlockRenderer()
    {
        OutputAttributesOnPre = false;
    }

    protected override void Write(
        HtmlRenderer renderer,
        CodeBlock codeBlock)
    {
        if (codeBlock is AnnotatedCodeBlock block)
        {
            block.EnsureInitialized();

            if (block.Diagnostics.Any())
            {
                renderer.WriteLine(@"<div class=""notification is-danger"">");
                renderer.WriteLine(SvgResources.ErrorSvg);

                foreach (var diagnostic in block.Diagnostics)
                {
                    renderer.WriteEscape("\t" + diagnostic);
                    renderer.WriteLine();
                }

                renderer.WriteLine(@"</div>");
            }
            else
            {
                block.RenderTo(
                    renderer,
                    InlineControls,
                    EnablePreviewFeatures);
            }
        }
        else
        {
            base.Write(renderer, codeBlock);
        }
    }

    public bool EnablePreviewFeatures { get; set; }

    public bool InlineControls { get; set; } = true;
}