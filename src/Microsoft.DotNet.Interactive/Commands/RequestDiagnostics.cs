// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestDiagnostics : KernelCommand
{
    public RequestDiagnostics(
        string code,
        string targetKernelName = null)
        : base(targetKernelName)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    internal RequestDiagnostics(TopLevelSyntaxNode syntaxNode)
        : base(syntaxNode.TargetKernelName)
    {
        Code = syntaxNode.Text;
        SyntaxNode = syntaxNode;

        if (syntaxNode is DirectiveNode { Kind: DirectiveNodeKind.Action } actionDirectiveNode)
        {
            TargetKernelName = actionDirectiveNode.TargetKernelName;
        }
    }

    public string Code { get; }

    internal TopLevelSyntaxNode SyntaxNode { get; }
}