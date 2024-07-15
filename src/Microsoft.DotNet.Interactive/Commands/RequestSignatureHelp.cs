// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestSignatureHelp : LanguageServiceCommand
{
    public RequestSignatureHelp(
        string code,
        LinePosition linePosition,
        string targetKernelName = null)
        : base(code, linePosition, targetKernelName)
    {
    }

    internal RequestSignatureHelp(
        TopLevelSyntaxNode syntaxNode,
        LinePosition linePosition, 
        int originalPosition)
        : base(syntaxNode, linePosition, originalPosition)
    {
    }

    internal override LanguageServiceCommand AdjustForCommandSplit(
        TopLevelSyntaxNode syntaxNode,
        LinePosition adjustedPosition,
        int originalPosition)
    {
        return new RequestSignatureHelp(syntaxNode, adjustedPosition, originalPosition);
    }
}