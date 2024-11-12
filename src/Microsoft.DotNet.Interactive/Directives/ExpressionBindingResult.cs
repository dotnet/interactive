// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Directives;

internal class ExpressionBindingResult
{
    public Dictionary<DirectiveParameterValueNode, object?> BoundValues { get; init; } = new();

    public CodeAnalysis.Diagnostic[] Diagnostics { get; init; } = [];

    public Dictionary<string, InputProduced> InputsProduced { get; set; } = new();

    public Dictionary<string, ValueProduced> ValuesProduced { get; set; } = new();
}