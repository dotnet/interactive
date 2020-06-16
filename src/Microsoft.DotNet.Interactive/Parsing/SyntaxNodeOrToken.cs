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
        private protected SyntaxNodeOrToken(
            SourceText sourceText, 
            PolyglotSyntaxTree? syntaxTree)
        {
            SyntaxTree = syntaxTree;
            SourceText = sourceText;
        }

        protected SourceText SourceText { get; }

        public SyntaxNode? Parent { get; internal set; }

        public abstract TextSpan Span { get; }

        public PolyglotSyntaxTree? SyntaxTree { get; }

        public string Text => SourceText.GetSubText(Span).ToString();
    }
}