// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public class HoverResponse
    {
        public MarkupContent Contents { get; set; }
        public Range Range { get; set; }

        public HoverResponse(MarkupContent contents, Range range = null)
        {
            Contents = contents;
            Range = range;
        }

        public static HoverResponse FromHoverEvent(HoverTextProduced responseEvent)
        {
            var markupContent = responseEvent switch
            {
                HoverMarkdownProduced markdown => new MarkupContent(MarkupKind.Markdown, markdown.Content),
                HoverPlainTextProduced plainText => new MarkupContent(MarkupKind.Plaintext, plainText.Content),
                _ => throw new NotSupportedException(),
            };
            return new HoverResponse(
                markupContent,
                Range.FromLinePositionSpan(responseEvent.Range));
        }
    }
}
