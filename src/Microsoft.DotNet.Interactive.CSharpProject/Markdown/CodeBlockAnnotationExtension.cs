// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public class CodeBlockAnnotationExtension : IMarkdownExtension
    {
        private readonly CodeFenceAnnotationsParser _annotationsParser;

        public CodeBlockAnnotationExtension(CodeFenceAnnotationsParser annotationsParser = null)
        {
            _annotationsParser = annotationsParser ?? new CodeFenceAnnotationsParser();
        }

        public bool InlineControls { get; set; } = true;

        public bool EnablePreviewFeatures { get; set; }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<AnnotatedCodeBlockParser>())
            {
                // It should execute before Markdig's default FencedCodeBlockParser
                pipeline.BlockParsers.Insert(
                    index: 0,
                    new AnnotatedCodeBlockParser(_annotationsParser));
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;

            var renderers = htmlRenderer?.ObjectRenderers;

            if (renderers != null && !renderers.Contains<AnnotatedCodeBlockRenderer>())
            {
                var annotatedCodeBlockRenderer = new AnnotatedCodeBlockRenderer();
                annotatedCodeBlockRenderer.EnablePreviewFeatures = EnablePreviewFeatures;
                annotatedCodeBlockRenderer.InlineControls = InlineControls;
                renderers.Insert(0, annotatedCodeBlockRenderer);
            }
        }
    }
}