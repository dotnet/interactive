// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public abstract class SyntaxNodeOrToken
    {
        private readonly SourceText _sourceText;

        private protected SyntaxNodeOrToken(
            SourceText sourceText, 
            PolyglotSyntaxTree? syntaxTree)
        {
            SyntaxTree = syntaxTree;
            _sourceText = sourceText;
        }

        public SyntaxNode? Parent { get; internal set; }

        public abstract TextSpan Span { get; }

        public PolyglotSyntaxTree? SyntaxTree { get; }


        public string Text => _sourceText.GetSubText(Span).ToString();
    }
}