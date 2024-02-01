// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Directives;

public class KernelActionDirective : KernelDirective
{
    // FIX: (KernelActionDirective) get rid of parameters vs named parameters distinction
    private readonly NamedSymbolCollection<KernelDirectiveNamedParameter> _namedParameters = new(directive => directive.Name);
    private readonly NamedSymbolCollection<KernelActionDirective> _subcommands = new(directive => directive.Name);
    private readonly NamedSymbolCollection<KernelDirectiveParameter> _parameters = new(parameter => parameter.Name);

    public KernelActionDirective(string name) : base(name)
    {
    }

    public Type DeserializeAs { get; set; }

    public ICollection<KernelDirectiveNamedParameter> NamedParameters => _namedParameters;

    public ICollection<KernelActionDirective> Subcommands => _subcommands;

    public ICollection<KernelDirectiveParameter> Parameters => _parameters;

    internal bool TryGetNamedParameter(string name, out KernelDirectiveNamedParameter value) => _namedParameters.TryGetValue(name, out value);

    internal bool TryGetParameter(string name, out KernelDirectiveParameter value) => _parameters.TryGetValue(name, out value);

    internal bool TryGetSubcommand(string name, out KernelActionDirective value) => _subcommands.TryGetValue(name, out value);
}