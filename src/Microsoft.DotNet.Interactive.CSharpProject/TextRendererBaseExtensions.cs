// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    internal static class TextRendererBaseExtensions
    {
        public static T WriteLineIf<T>(this T textRendererBase, bool @if, string value)
            where T : HtmlRenderer
        {
            if (@if)
            {
                textRendererBase.WriteLine(value);
            }

            return textRendererBase;
        }
    }
}