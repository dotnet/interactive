// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class SubmitCode : KernelCommand
{
    public SubmitCode(
        string code,
        string targetKernelName = null) 
        : base(targetKernelName)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }
      
    internal SubmitCode(
        TopLevelSyntaxNode syntaxNode,
        DirectiveNode directiveNode = null)
        : base(syntaxNode.TargetKernelName)
    {
        Code = syntaxNode.Text;
        SyntaxNode = syntaxNode;
        DirectiveNode = directiveNode;
        SchedulingScope = SchedulingScope.Parse(syntaxNode.CommandScope);

        if (syntaxNode is DirectiveNode { Kind: DirectiveNodeKind.Action } actionDirectiveNode)
        {
            TargetKernelName = actionDirectiveNode.TargetKernelName;
        }
    }

    public string Code { get; internal set; }

    public override string ToString() => $"{nameof(SubmitCode)}: {Code?.TruncateForDisplay()}";

    internal TopLevelSyntaxNode SyntaxNode { get; }

    internal DirectiveNode DirectiveNode { get; }
}