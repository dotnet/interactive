// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public class DirectiveArgsToken : SyntaxToken
    {
        internal DirectiveArgsToken(
            SourceText text,
            TextSpan span,
            PolyglotSyntaxTree? syntaxTree) : base(text, span, syntaxTree)
        {
        }
    }
}