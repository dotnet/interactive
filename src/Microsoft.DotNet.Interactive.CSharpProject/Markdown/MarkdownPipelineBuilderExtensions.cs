// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

public static class MarkdownPipelineBuilderExtensions
{
    public static MarkdownPipelineBuilder UseCodeBlockAnnotations(
        this MarkdownPipelineBuilder pipeline,
        CodeFenceAnnotationsParser annotationsParser = null,
        bool inlineControls = true)
    {
        var extensions = pipeline.Extensions;

        if (!extensions.Contains<CodeBlockAnnotationExtension>())
        {
            extensions.Add(new CodeBlockAnnotationExtension(annotationsParser)
            {
                InlineControls = inlineControls
            });
        }

        return pipeline;
    }
}