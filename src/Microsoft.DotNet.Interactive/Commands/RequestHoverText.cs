// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestHoverText : LanguageServiceCommand
{
    public RequestHoverText(
        string code,
        LinePosition linePosition,
        string targetKernelName = null)
        : base(code, linePosition, targetKernelName)
    {
    }

    internal RequestHoverText(
        TopLevelSyntaxNode syntaxNode,
        LinePosition adjustedPosition,
        int originalPosition)
        : base(syntaxNode, adjustedPosition, originalPosition)
    {
    }

    internal override LanguageServiceCommand AdjustForCommandSplit(
        TopLevelSyntaxNode syntaxNode,
        LinePosition adjustedPosition,
        int originalPosition)
    {
        return new RequestHoverText(syntaxNode, adjustedPosition, originalPosition);
    }
}