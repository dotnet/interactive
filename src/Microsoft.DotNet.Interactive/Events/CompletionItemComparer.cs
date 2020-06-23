// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Events
{
    internal class CompletionItemComparer : IEqualityComparer<CompletionItem>
    {
        public static IEqualityComparer<CompletionItem> Instance { get; } = new CompletionItemComparer();

        public bool Equals(CompletionItem x, CompletionItem y) =>
            string.Equals(x?.DisplayText,
                          y?.DisplayText,
                          StringComparison.CurrentCulture);

        public int GetHashCode(CompletionItem obj)
        {
            return obj.DisplayText.GetHashCode();
        }
    }
}