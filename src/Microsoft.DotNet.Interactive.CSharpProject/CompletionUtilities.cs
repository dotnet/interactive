// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public static class CompletionUtilities
    {
        public static IEnumerable<CompletionItem> Deduplicate(this IEnumerable<CompletionItem> source)
        {
            return source.Distinct(CompletionItemEqualityComparer.Instance);
        }

        private class CompletionItemEqualityComparer : IEqualityComparer<CompletionItem>
        {
            private CompletionItemEqualityComparer()
            {
            }

            public static CompletionItemEqualityComparer Instance { get; } = new CompletionItemEqualityComparer();

            public bool Equals(CompletionItem x, CompletionItem y)
            {
                return x.Kind.Equals(y.Kind) &&
                       x.InsertText.Equals(y.InsertText);
            }

            public int GetHashCode(CompletionItem obj)
            {
                return (obj.Kind + obj.InsertText).GetHashCode();
            }
        }
    }
}
