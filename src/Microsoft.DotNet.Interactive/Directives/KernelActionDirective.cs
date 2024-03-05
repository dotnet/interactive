// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

    public Type? KernelCommandType { get; set; }

    public ICollection<KernelActionDirective> Subcommands => _subcommands;

    public override IEnumerable<KernelDirectiveParameter> BindableParameters
    {
        get
        {
            foreach (var parameter in Parameters)
            {
                yield return parameter;
            }

            if (Parent is not null)
            {
                foreach (var parentParameter in Parent.BindableParameters)
                {
                    yield return parentParameter;
                }
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

    internal override bool TryGetParameter(string name, out KernelDirectiveParameter value)
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