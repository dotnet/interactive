// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Directives;

public class KernelDirectiveParameter
{
    List<Func<KernelDirectiveCompletionContext, Task<IEnumerable<CompletionItem>>>>? _completionSources;

    public KernelDirectiveParameter(string name, string? description = null)
    {
        Name = name;
        Description = description ?? string.Empty;
    }

    public string Name { get; }

    public string Description { get; init; }

    public bool AllowImplicitName { get; init; }

    public int MaxOccurrences { get; init; } = 1;

    public bool Required { get; init; } = false;

    public string TypeHint { get; init; } = "text";

    public bool Flag { get; set; }

    public KernelDirectiveParameter AddCompletions(Func<KernelDirectiveCompletionContext, IEnumerable<CompletionItem>> getCompletions)
    {
        _completionSources ??= new();

        _completionSources.Add(context => Task.FromResult(getCompletions(context)));

        return this;
    }

    public KernelDirectiveParameter AddCompletions(Func<KernelDirectiveCompletionContext, Task<IEnumerable<CompletionItem>>> getCompletions)
    {
        _completionSources ??= new();

        _completionSources.Add(getCompletions);

        return this;
    }

    public KernelDirectiveParameter AddCompletions(Func<KernelDirectiveCompletionContext, IEnumerable<string>> getCompletions)
    {
        _completionSources ??= new();

        _completionSources.Add(context =>
                                   Task.FromResult(getCompletions(context)
                                                       .Select(s => new CompletionItem(s, WellKnownTags.Parameter))));

        return this;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetValueCompletionsAsync()
    {
        if (_completionSources is null)
        {
            return [];
        }

        var context = new KernelDirectiveCompletionContext();

        foreach (var source in _completionSources)
        {
            var completionItems = await source(context);
            foreach (var item in completionItems)
            {
                context.Completions.Add(item);
            }
        }

        foreach (var completion in context.Completions)
        {
            completion.AssociatedSymbol = this;
            completion.Documentation = Description;
        }

        return context.Completions.ToArray();
    }

    public override string ToString() => Name;
}