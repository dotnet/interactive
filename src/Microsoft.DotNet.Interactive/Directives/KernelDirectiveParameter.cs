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
    List<Func<KernelDirectiveCompletionContext, Task>>? _completionSources;

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

    public bool Flag { get; init; }

    public KernelDirectiveParameter AddCompletions(
        Func<KernelDirectiveCompletionContext, Task> getCompletions)
    {
        _completionSources ??= new();

        _completionSources.Add(getCompletions);

        return this;
    }

    public KernelDirectiveParameter AddCompletions(Func<IEnumerable<string>> getCompletions)
    {
        _completionSources ??= new();

        _completionSources.Add(context =>
        {
            var completionItems = getCompletions().Select(s => new CompletionItem(s, WellKnownTags.Parameter));
            foreach (var item in completionItems)
            {
                context.CompletionItems.Add(item);
            }

            return Task.CompletedTask;
        });

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
            await source(context);
        }

        foreach (var completion in context.CompletionItems)
        {
            completion.AssociatedSymbol = this;
            completion.Documentation = Description;
        }

        return context.CompletionItems.ToArray();
    }

    public override string ToString() => Name;
}