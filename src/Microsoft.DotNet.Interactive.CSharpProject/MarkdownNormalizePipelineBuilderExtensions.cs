// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Markdig;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public static class MarkdownNormalizePipelineBuilderExtensions
    {
        public static MarkdownPipelineBuilder UseNormalizeCodeBlockAnnotations(
            this MarkdownPipelineBuilder pipeline,
            Dictionary<string, string> outputsBySessionName)
        {
            var extensions = pipeline.Extensions;

            if (!extensions.Contains<NormalizeBlockAnnotationExtension>())
            {
                extensions.Add(new NormalizeBlockAnnotationExtension(outputsBySessionName));
            }

            if (!extensions.Contains<SkipEmptyLinkReferencesExtension>())
            {
                extensions.Add(new SkipEmptyLinkReferencesExtension());
            }

            return pipeline;
        }
    }
}