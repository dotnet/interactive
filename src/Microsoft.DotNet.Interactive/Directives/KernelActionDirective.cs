// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Directives;

public partial class KernelActionDirective : KernelDirective
{
    private readonly NamedSymbolCollection<KernelActionDirective> _subcommands;
    private KernelActionDirective? _parent;

    public KernelActionDirective(string name) : base(name)
    {
        _subcommands = new NamedSymbolCollection<KernelActionDirective>(
            directive => directive.Name,
            (adding, existing) =>
            {
                if (existing.Any(item => item.Name == adding.Name))
                {
                    throw new ArgumentException($"Directive already contains a subcommand named '{adding.Name}'.");
                }

                if (adding.Parent is not null)
                {
                    throw new ArgumentException("Directives cannot be reparented.");
                }

                if (Parent is not null || adding.Subcommands.Any())
                {
                    throw new ArgumentException("Only one level of directive subcommands is allowed.");
                }

                adding.Parent = this;
            });

    }

    [JsonIgnore]
    public Type? KernelCommandType { get; set; }

    public ICollection<KernelActionDirective> Subcommands
    {
        get => _subcommands;
        init
        {
            if (value is null)
            {
                return;
            }

            foreach (var directive in value)
            {
                _subcommands.Add(directive);
            }
        }
    }

    public KernelActionDirective? Parent
    {
        get => _parent;
        private set
        {
            if (_parent is not null)
            {
                throw new InvalidOperationException("Parent cannot be changed once it has been set.");
            }

            _parent = value;
        }
    }

    public override async Task<IReadOnlyList<CompletionItem>> GetChildCompletionsAsync()
    {
        var baseCompletions = await base.GetChildCompletionsAsync();

        var subcommandCompletions = Subcommands.Select(s => new CompletionItem(s.Name, WellKnownTags.Method)
        {
            AssociatedSymbol = s
        });

        return subcommandCompletions.Concat(baseCompletions).ToArray();
    }

    internal override bool TryGetParameter(
        string name, 
        [MaybeNullWhen(false)] out KernelDirectiveParameter value)
    {
        if (base.TryGetParameter(name, out value))
        {
            return true;
        }

        if (Parent is not null)
        {
            return Parent.TryGetParameter(name, out value);
        }

        return false;
    }

    internal bool TryGetSubcommand(string name, [MaybeNullWhen(false)] out KernelActionDirective value) =>
        _subcommands.TryGetValue(name, out value);
}