// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Directives;

public class KernelActionDirective : KernelDirective
{
    private readonly NamedSymbolCollection<KernelActionDirective> _subcommands;
    private readonly NamedSymbolCollection<KernelDirectiveParameter> _parameters;

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

                adding.Parent = this;
            });

        _parameters = new NamedSymbolCollection<KernelDirectiveParameter>(
            parameter => parameter.Name,
            (adding, existing) =>
            {
                if (existing.Any(item => item.Name == adding.Name))
                {
                    throw new ArgumentException($"Directive already contains a parameter named '{adding.Name}'.");
                }

                if (adding.AllowImplicitName && existing.Any(item => item.AllowImplicitName))
                {
                    throw new ArgumentException($"Only one parameter on a directive can have {nameof(KernelDirectiveParameter.AllowImplicitName)} set to true.");
                }
            });
    }

    public Type? DeserializeAs { get; set; }

    public ICollection<KernelActionDirective> Subcommands => _subcommands;

    public ICollection<KernelDirectiveParameter> Parameters => _parameters;

    public IEnumerable<KernelDirectiveParameter> ParametersIncludingAncestors
    {
        get
        {
            foreach (var parameter in Parameters)
            {
                yield return parameter;
            }

            if (Parent is not null)
            {
                foreach (var parentParameter in Parent.ParametersIncludingAncestors)
                {
                    yield return parentParameter;
                }
            }
        }
    }

    public KernelActionDirective? Parent { get; private set; }

    internal bool TryGetParameter(string name, [MaybeNullWhen(false)] out KernelDirectiveParameter value) => _parameters.TryGetValue(name, out value);

    internal bool TryGetSubcommand(string name, [MaybeNullWhen(false)] out KernelActionDirective value) => _subcommands.TryGetValue(name, out value);
}