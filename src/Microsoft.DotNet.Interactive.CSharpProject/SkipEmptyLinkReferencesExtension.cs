// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public class SkipEmptyLinkReferencesExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is NormalizeRenderer)
            {
                renderer.ObjectRenderers.Replace<LinkReferenceDefinitionRenderer>(new SkipEmptyLinkReferencesRender());
            }
        }

        public class SkipEmptyLinkReferencesRender : LinkReferenceDefinitionRenderer
        {
            protected override void Write(NormalizeRenderer renderer, LinkReferenceDefinition linkDef)
            {
                if (linkDef.Label == null && linkDef.Url == null)
                    return;

                base.Write(renderer, linkDef);
            }
        }
    }
}