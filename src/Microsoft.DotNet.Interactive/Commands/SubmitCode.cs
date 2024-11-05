// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class SubmitCode : KernelCommand
{
    private Dictionary<string, string> _parameters;

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

        if (directiveNode is { HasParameters: true, Kind: DirectiveNodeKind.KernelSelector })
        {
            _parameters = directiveNode.GetParameterValues(
                                               new Dictionary<DirectiveParameterValueNode, object>())
                                           .ToDictionary(t => t.Name, t => t.Value?.ToString());
        }
        else if (syntaxNode is DirectiveNode { Kind: DirectiveNodeKind.Action } actionDirectiveNode)
        {
            TargetKernelName = actionDirectiveNode.TargetKernelName;
        }
    }

    public string Code { get; internal set; }

    public IDictionary<string, string> Parameters
    {
        get
        {
            _parameters ??= new();
            return _parameters;
        }
        init
        {
            _parameters ??= new();
            if (value is not null)
            {
                foreach (var pair in value)
                {
                    _parameters.Add(pair.Key, pair.Value);
                }
            }
        }
    }

    public override string ToString() => $"{nameof(SubmitCode)}: {Code?.TruncateForDisplay()}";

    internal TopLevelSyntaxNode SyntaxNode { get; }

    internal DirectiveNode DirectiveNode { get; }
}