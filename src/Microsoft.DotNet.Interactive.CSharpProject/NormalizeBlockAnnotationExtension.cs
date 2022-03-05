// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Markdig;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public class NormalizeBlockAnnotationExtension : IMarkdownExtension
    {
        private readonly Dictionary<string, string> _outputsBySessionName;

        public NormalizeBlockAnnotationExtension(Dictionary<string, string> outputsBySessionName)
        {
            _outputsBySessionName = outputsBySessionName;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is NormalizeRenderer)
            {
                renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new AnnotatedCodeBlockRenderer(_outputsBySessionName));
            }
        }

        public class AnnotatedCodeBlockRenderer : CodeBlockRenderer
        {
            private readonly Dictionary<string, string> _outputsBySessionName;

            public AnnotatedCodeBlockRenderer(Dictionary<string, string> outputsBySessionName)
            {
                _outputsBySessionName = outputsBySessionName;
            }

            protected override void Write(
                NormalizeRenderer renderer,
                CodeBlock codeBlock)
            {
                if (codeBlock is AnnotatedCodeBlock block)
                {
                    if (block.Annotations is CodeBlockAnnotations codeBlockAnnotations)
                    {
                        block.Arguments = $"{block.Annotations.Language} {codeBlockAnnotations.RunArgs}";
                    }

                    if (block.Annotations is OutputBlockAnnotations outputBlockAnnotations)
                    {
                        block.Arguments = $"{block.Annotations.Language} {outputBlockAnnotations.RunArgs}";

                        var output = _outputsBySessionName[outputBlockAnnotations.Session];

                        block.Lines = new StringLineGroup(output);
                    }
                }

                base.Write(renderer, codeBlock);
            }
        }
    }
}