// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestCompletions : LanguageServiceCommand
{
    public RequestCompletions(
        string code,
        LinePosition linePosition, 
        string targetKernelName = null)
        : base(code, linePosition, targetKernelName)
    {
    }

    internal RequestCompletions(
        TopLevelSyntaxNode syntaxNode,
        LinePosition linePosition) 
        : base(syntaxNode, linePosition)
    {
    }

    internal override LanguageServiceCommand With(
        TopLevelSyntaxNode syntaxNode,
        LinePosition position)
    {
        return new RequestCompletions(syntaxNode, position);
    }
}