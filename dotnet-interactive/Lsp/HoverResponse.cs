// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public static HoverResponse FromLanguageServiceEvent(Events.LanguageServiceHoverResponseProduced responseEvent)
        {
            return new HoverResponse(
                MarkupContent.FromLanguageServiceMarkupContent(responseEvent.Contents),
                Range.FromLanguageServiceRange(responseEvent.Range));
        }
    }
}
