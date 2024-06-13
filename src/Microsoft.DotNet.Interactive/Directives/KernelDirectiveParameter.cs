// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Directives;

public class KernelDirectiveParameter
{
    public KernelDirectiveParameter(string name, string? description = null)
    {
        Name = name;
        Description = description ?? string.Empty;
    }

    public string Name { get; }

    public string Description { get; set; }

    public bool AllowImplicitName { get; init; }

    public int MaxOccurrences { get; init; } = 1;

    public bool Required { get; init; } = false;

    public string TypeHint { get; init; } = "text";

    public bool Flag { get; set; }

    public void AddCompletions(Func<KernelDirectiveCompletionContext, IEnumerable<CompletionItem>> getCompletions)
    {
        // FIX: (AddCompletions) 
    }

    public void AddCompletions(Func<KernelDirectiveCompletionContext, IEnumerable<string>> getCompletions)
    {
        // FIX: (AddCompletions) 
    }

    public void AddCompletions(IEnumerable<CompletionItem> getCompletions)
    {
        // FIX: (AddCompletions) 
    }

    public IReadOnlyList<CompletionItem> GetCompletions()
    {
        // FIX: (GetCompletions) 
        return [];
    }
}