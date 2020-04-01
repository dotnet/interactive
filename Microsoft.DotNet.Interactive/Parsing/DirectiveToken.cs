// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public class DirectiveToken : SyntaxToken
    {
        public DirectiveToken(
            SourceText text,
            TextSpan span) : base(text, span)
        {
            DirectiveName = Text.Replace("#!", "");
        }

        public string DirectiveName { get; }
    }
}