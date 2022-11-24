// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models.SignatureHelp;

public class InvocationContext
{
    public SemanticModel SemanticModel { get; }
    public int Position { get; }
    public SyntaxNode Receiver { get; }
    public IEnumerable<TypeInfo> ArgumentTypes { get; }
    public IEnumerable<SyntaxToken> Separators { get; }

    public InvocationContext(SemanticModel semanticModel, int position, SyntaxNode receiver, ArgumentListSyntax argList)
    {
        SemanticModel = semanticModel;
        Position = position;
        Receiver = receiver;
        ArgumentTypes = argList.Arguments.Select(argument => semanticModel.GetTypeInfo(argument.Expression));
        Separators = argList.Arguments.GetSeparators();
    }

    public InvocationContext(SemanticModel semanticModel, int position, SyntaxNode receiver, AttributeArgumentListSyntax argList)
    {
        SemanticModel = semanticModel;
        Position = position;
        Receiver = receiver;
        ArgumentTypes = argList.Arguments.Select(argument => semanticModel.GetTypeInfo(argument.Expression));
        Separators = argList.Arguments.GetSeparators();
    }
}