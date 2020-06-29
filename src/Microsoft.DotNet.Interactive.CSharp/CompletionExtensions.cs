// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive.Events;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal static class CompletionExtensions
    {
        private static readonly ImmutableArray<string> KindTags = ImmutableArray.Create(
            WellKnownTags.Class,
            WellKnownTags.Constant,
            WellKnownTags.Delegate,
            WellKnownTags.Enum,
            WellKnownTags.EnumMember,
            WellKnownTags.Event,
            WellKnownTags.ExtensionMethod,
            WellKnownTags.Field,
            WellKnownTags.Interface,
            WellKnownTags.Intrinsic,
            WellKnownTags.Keyword,
            WellKnownTags.Label,
            WellKnownTags.Local,
            WellKnownTags.Method,
            WellKnownTags.Module,
            WellKnownTags.Namespace,
            WellKnownTags.Operator,
            WellKnownTags.Parameter,
            WellKnownTags.Property,
            WellKnownTags.RangeVariable,
            WellKnownTags.Reference,
            WellKnownTags.Structure,
            WellKnownTags.TypeParameter);

        public static string GetKind(this RoslynCompletionItem completionItem)
        {
            foreach (var tag in KindTags)
            {
                if (completionItem.Tags.Contains(tag))
                {
                    return tag;
                }
            }

            return null;
        }

        public static CompletionItem ToModel(this RoslynCompletionItem item)
        {
            return new CompletionItem(
                displayText: item.DisplayText,
                kind: item.GetKind(),
                filterText: item.FilterText,
                sortText: item.SortText,
                insertText: item.FilterText);
        }
    }
}