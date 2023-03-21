// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive.Events;
using RoslynCompletionDescription = Microsoft.CodeAnalysis.Completion.CompletionDescription;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.DotNet.Interactive.CSharp;

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
        WellKnownTags.TypeParameter,
        WellKnownTags.Snippet,
        WellKnownTags. Error,
        WellKnownTags.Warning);

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

    public static CompletionItem ToModel(this RoslynCompletionItem item, RoslynCompletionDescription description)
    {
        var isGeneric =
            item.Properties.TryGetValue("IsGeneric", out var isGenericProperty) &&
            bool.TryParse(isGenericProperty, out var isGenericResult) &&
            isGenericResult;

        var isMethod =
            item.Tags.Contains(WellKnownTags.Method) ||
            item.Tags.Contains(WellKnownTags.ExtensionMethod);

        var (displayTextSuffix, insertTextSuffix) = (isGeneric, isMethod) switch
        {
            (true, true) => ("<>", "<$1>($2)"),
            (true, false) => ("<>", "<$1>"),
            (false, true) => ("", "($1)"),
            (false, false) => ("", ""),
        };

        var displayText = item.DisplayText + displayTextSuffix;
        var insertText = item.FilterText + insertTextSuffix;

        InsertTextFormat? insertTextFormat = isGeneric || isMethod
            ? InsertTextFormat.Snippet
            : null;
        return new CompletionItem(
            displayText: displayText,
            kind: item.GetKind(),
            filterText: item.FilterText,
            sortText: item.SortText,
            insertText: insertText,
            insertTextFormat: insertTextFormat,
            documentation: description.Text);
    }
}