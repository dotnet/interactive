﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CompletionItem
    {
        public CompletionItem(string displayText, string kind, string filterText = null, string sortText = null, string insertText = null, InsertTextFormat? insertTextFormat = null, string documentation = null)
        {
            DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
            Kind = kind ?? throw new ArgumentException(nameof(kind));
            FilterText = filterText;
            SortText = sortText;
            InsertText = insertText;
            InsertTextFormat = insertTextFormat;
            Documentation = documentation;
        }

        public string DisplayText { get; }

        public string Kind { get; }

        public string FilterText { get; }

        public string SortText { get; }

        public string InsertText { get; }

        public InsertTextFormat? InsertTextFormat { get; }

        public string Documentation { get; set; }

        public override string ToString() => DisplayText;
    }
}