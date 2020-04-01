// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class PolyglotSyntaxTree
    {
        private readonly SourceText _sourceText;
        private readonly SyntaxNode _root;

        public PolyglotSyntaxTree(
            SourceText sourceText,
            SyntaxNode root)
        {
            _sourceText = sourceText;
            _root = root;
        }

        public int Length => _sourceText.Length;

        public SyntaxNode GetRoot()
        {
            return _root;
        }

        public bool TryGetText(out SourceText text)
        {
            text = default;
            return false;
        }

        public SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
        {
            return default;
        }

        protected Task<SyntaxNode> GetRootAsyncCore(CancellationToken cancellationToken)
        {
            return default;
        }


        public IEnumerable<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = new CancellationToken())
        {
            return default;
        }

        public IEnumerable<Diagnostic> GetDiagnostics(SyntaxNode node)
        {
            return default;
        }

        public IEnumerable<Diagnostic> GetDiagnostics(SyntaxToken token)
        {
            return default;
        }

        public IEnumerable<Diagnostic> GetDiagnostics(SyntaxTrivia trivia)
        {
            return default;
        }

        public IEnumerable<Diagnostic> GetDiagnostics(CodeAnalysis.SyntaxNodeOrToken nodeOrToken)
        {
            return default;
        }

        public FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = new CancellationToken())
        {
            return default;
        }

        public override string ToString()
        {
            return _sourceText.ToString();
        }

        public string? GetLanguageAtPosition(int position)
        {
            if (position >= _root.Span.End)
            {
                position = _root.Span.End - 1;
            }

            var node = _root.FindNode(position);

            switch (node)
            {
                case LanguageNode languageNode:
                    return languageNode.Language;

                case PolyglotSubmissionNode submissionNode:
                    return submissionNode.DefaultLanguage;

                default:
                    return null;
            }
        }
    }
}