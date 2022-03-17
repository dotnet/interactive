// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class CompletionItem
    {
        public string DisplayText { get; }
        public string Kind { get; }
        public string FilterText { get; }
        public string SortText { get; }
        public string InsertText { get; }
        public MarkdownString Documentation { get; set; }
        public Uri AcceptanceUri { get; }
        public CompletionItem(string displayText, string kind, string filterText = null, string sortText = null, string insertText = null, MarkdownString documentation = null, Uri acceptanceUri = null)
        {
            DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
            Kind = kind ?? throw new ArgumentException(nameof(kind));
            FilterText = filterText;
            SortText = sortText;
            InsertText = insertText;
            Documentation = documentation;
            AcceptanceUri = acceptanceUri;
        }

        public override string ToString() => DisplayText;
    }
}
