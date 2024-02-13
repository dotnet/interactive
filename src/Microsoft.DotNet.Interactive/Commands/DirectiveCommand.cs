// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

internal class DirectiveCommand : KernelCommand
{
    internal DirectiveCommand(DirectiveNode directiveNode)
    {
        DirectiveNode = directiveNode ?? throw new ArgumentNullException(nameof(directiveNode));
        SchedulingScope = SchedulingScope.Parse(directiveNode.CommandScope);
    }

    public DirectiveNode DirectiveNode { get; }

    internal override bool IsHidden => true;

    public override string ToString() => $"Directive: {DirectiveNode}";
}