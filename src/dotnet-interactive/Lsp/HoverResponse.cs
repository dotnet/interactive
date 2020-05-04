// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public class HoverResponse
    {
        private static Dictionary<string, int> PreferredContentTypePriority = new Dictionary<string, int>()
        {
            { "text/markdown", 1 },
            { "text/x-markdown", 1 },
            { "text/plain", 2 },
        };

        public MarkupContent Contents { get; set; }
        public Range Range { get; set; }

        public HoverResponse(MarkupContent contents, Range range = null)
        {
            Contents = contents;
            Range = range;
        }

        public static HoverResponse FromHoverEvent(HoverTextProduced responseEvent)
        {
            var preferredContent = responseEvent.Content.OrderBy(c => PriorityFromMimeType(c.MimeType)).First();
            return new HoverResponse(new MarkupContent(MarkupKindFromMimeType(preferredContent.MimeType), preferredContent.Value));
        }

        private static int PriorityFromMimeType(string mimeType)
        {
            if (PreferredContentTypePriority.TryGetValue(mimeType, out var priority))
            {
                return priority;
            }

            return 99;
        }

        private static MarkupKind MarkupKindFromMimeType(string mimeType)
        {
            switch (mimeType)
            {
                case "text/markdown":
                case "text/x-markdown":
                    return MarkupKind.Markdown;
                default:
                    return MarkupKind.Plaintext;
            }
        }
    }
}
