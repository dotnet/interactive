// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Utility;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    public abstract class SyntaxNode : 
        SyntaxNodeOrToken, 
        IEnumerable<SyntaxNodeOrToken>
    {
        private readonly List<SyntaxNodeOrToken> _childNodesAndTokens = new List<SyntaxNodeOrToken>();
        private TextSpan _span;

        private protected SyntaxNode(
            SourceText sourceText,
            PolyglotSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
        {
        }

        public override TextSpan Span => _span;

        public bool Contains(SyntaxNode node) => false;

        public SyntaxNode? FindNode(TextSpan span) =>
            DescendantNodesAndTokensAndSelf()
                .OfType<SyntaxNode>()
                .Reverse()
                .FirstOrDefault(n => n.Span.Contains(span));

        public SyntaxNode? FindNode(int position) =>
            FindToken(position)?.Parent;

        public SyntaxToken? FindToken(int position)
        {
            var candidate = _childNodesAndTokens.FirstOrDefault(n => n.Span.Contains(position));

            return candidate switch
            {
                SyntaxNode node => node.FindToken(position),
                SyntaxToken token => token,
                _ => null
            };
        }

        internal void Add(SyntaxNodeOrToken child)
        {
            if (child.Parent != null)
            {
                throw new InvalidOperationException($"{child.GetType().Name} {child} is already parented to {child.Parent}");
            }

            child.Parent = this;

            if (_span == default)
            {
                _span = child.Span;
            }
            else
            {
                var _spanStart = Math.Min(_span.Start, child.Span.Start);
                var _spanEnd = Math.Max(_span.End, child.Span.End);
                _span = new TextSpan(_spanStart, _spanEnd - _span.Start);
            }

            _childNodesAndTokens.Add(child);
        }

        public IEnumerable<SyntaxNodeOrToken> ChildNodes => _childNodesAndTokens.OfType<SyntaxNode>();

        public IEnumerable<SyntaxNodeOrToken> ChildTokens => _childNodesAndTokens.OfType<SyntaxToken>();

        public IEnumerable<SyntaxNodeOrToken> ChildNodesAndTokens => _childNodesAndTokens;

        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensAndSelf()
        {
            yield return this;

            foreach (var syntaxNodeOrToken1 in DescendantNodesAndTokens())
            {
                yield return syntaxNodeOrToken1;
            }
        }

        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens() =>
            _childNodesAndTokens
                .FlattenDepthFirst(n => n switch
                {
                    SyntaxNode node => node.ChildNodesAndTokens,
                    _ => Array.Empty<SyntaxNodeOrToken>()
                });

        public virtual IEnumerable<Diagnostic> GetDiagnostics()
        {
            yield break;
        } 

        public IEnumerator<SyntaxNodeOrToken> GetEnumerator()
        {
            return _childNodesAndTokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}