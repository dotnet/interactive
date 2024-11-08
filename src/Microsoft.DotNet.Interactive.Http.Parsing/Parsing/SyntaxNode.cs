// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal abstract class SyntaxNode : SyntaxNodeOrToken
{
    private TextSpan _fullSpan;
    private readonly List<SyntaxNodeOrToken> _childNodesAndTokens = new();
    private TextSpan _span;
    private bool _isSignificant = false;

    private protected SyntaxNode(
        SourceText sourceText,
        SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override TextSpan FullSpan => _fullSpan;

    public override bool IsSignificant => _isSignificant;

    public override TextSpan Span => _span;

    /// <summary>
    /// Gets the text of the current node, including trivia.
    /// </summary>
    public string FullText => SourceText.ToString(FullSpan);

    public SyntaxNode? FindNode(int position) =>
        FindToken(position)?.Parent;

    public SyntaxToken? FindToken(int position)
    {
        if (position == _fullSpan.End)
        {
            position--;
        }

        for (var i = 0; i < _childNodesAndTokens.Count; i++)
        {
            var nodeOrToken = _childNodesAndTokens[i];

            switch (nodeOrToken)
            {
                case SyntaxNode node:
                    if (node.FullSpan.Contains(position))
                    {
                        var token = node.FindToken(position);
                        return token;
                    }
                    break;
                case SyntaxToken token:
                    if (token.FullSpan.Contains(position))
                    {
                        return token;
                    }
                    break;
            }
        }

        return null;
    }

    private void GrowSpan(SyntaxNodeOrToken child)
    {
        if (_fullSpan == default)
        {
            _fullSpan = child.FullSpan;
            _span = child.Span;
        }
        else
        {
            // if the child span is empty and uninitialized, then set it to the end of the current node's span.
            if (child.FullSpan is { Start: 0, End: 0 })
            {
                if (child is SyntaxNode childNode)
                {
                    childNode._fullSpan = new TextSpan(FullSpan.End, 0);
                }
            }

            var fullSpanStart = Math.Min(_fullSpan.Start, child.FullSpan.Start);
            var fullSpanEnd = Math.Max(_fullSpan.End, child.FullSpan.End);
            _fullSpan = new TextSpan(fullSpanStart, fullSpanEnd - fullSpanStart);

            var firstSignificantNodeOrToken = ChildNodesAndTokens
                .FirstOrDefault(n => n.IsSignificant);

            var lastSignificantNodeOrToken = ChildNodesAndTokens
                .LastOrDefault(n => n.IsSignificant);

            var startOfSignificantText =
                firstSignificantNodeOrToken?.Span.Start ??
                FullSpan.Start;

            var endOfSignificantText =
                lastSignificantNodeOrToken?.Span.End ??
                FullSpan.End;

            _span = TextSpan.FromBounds(
                startOfSignificantText,
                endOfSignificantText);
        }
    }

    internal void Add(SyntaxToken token) => AddInternal(token);

    protected void AddInternal(SyntaxNodeOrToken child, bool addBefore = false)
    {
        if (child is null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (child.Parent is not null)
        {
            throw new InvalidOperationException($"{child.GetType().Name} {child} is already parented to {child.Parent}");
        }

        child.Parent = this;

        if (child.IsSignificant)
        {
            _isSignificant = true;
        }

        if (addBefore)
        {
            _childNodesAndTokens.Insert(0, child);
        }
        else
        {
            _childNodesAndTokens.Add(child);
        }

        GrowSpan(child);
        Parent?.GrowSpan(child);
    }

    public IEnumerable<SyntaxNode> ChildNodes =>
        _childNodesAndTokens.OfType<SyntaxNode>();

    public IEnumerable<SyntaxToken> ChildTokens => _childNodesAndTokens.OfType<SyntaxToken>();

    public IReadOnlyList<SyntaxNodeOrToken> ChildNodesAndTokens => _childNodesAndTokens;

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var child in ChildNodes)
        {
            foreach (var diagnostic in child.GetDiagnostics())
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }
    }

    public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensAndSelf()
    {
        yield return this;

        foreach (var node in DescendantNodesAndTokens())
        {
            yield return node;
        }
    }

    public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens() =>
        FlattenBreadthFirst(_childNodesAndTokens, n => n switch
        {
            SyntaxNode node => node.ChildNodesAndTokens,
            _ => Array.Empty<SyntaxNodeOrToken>()
        });

    public SyntaxNode? NextNode()
    {
        if (Parent is null)
        {
            return null;
        }

        var next = false;

        foreach (var sibling in Parent.ChildNodes)
        {
            if (next)
            {
                return sibling;
            }
            else if (sibling == this)
            {
                next = true;
            }
        }

        return null;
    }

    private static IEnumerable<T> FlattenBreadthFirst<T>(
        IEnumerable<T> source,
        Func<T, IEnumerable<T>> children)
    {
        var queue = new Queue<T>();

        foreach (var item in source)
        {
            queue.Enqueue(item);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var item in children(current))
            {
                queue.Enqueue(item);
            }

            yield return current;
        }
    }
}
