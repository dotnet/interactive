// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Directives;

public abstract partial class KernelDirective
{
    private readonly NamedSymbolCollection<KernelDirectiveParameter> _parameters;

    [JsonConstructor]
    protected KernelDirective(string name)
    {
        Name = name;

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

    public string Name { get; init; }
    
    public string? Description { get; set; }

    public ICollection<KernelDirectiveParameter> Parameters => _parameters;

    internal KernelInfo? ParentKernelInfo { get; set; }

    public virtual IEnumerable<KernelDirectiveParameter> BindableParameters => Parameters;

    public override string ToString() => Name;

    internal virtual bool TryGetParameter(string name, [MaybeNullWhen(false)] out KernelDirectiveParameter value) =>
        _parameters.TryGetValue(name, out value);
}