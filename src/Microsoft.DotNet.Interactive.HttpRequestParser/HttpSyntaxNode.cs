// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal abstract class HttpSyntaxNode : HttpSyntaxNodeOrToken
{
    private TextSpan _fullSpan;
    private readonly List<HttpSyntaxNodeOrToken> _childNodesAndTokens = new();

    private protected HttpSyntaxNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override TextSpan FullSpan => _fullSpan;

    public override bool IsSignificant
    {
        get
        {
            foreach (var child in ChildNodesAndTokens)
            {
                if (child.IsSignificant)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public override string Text => SourceText.ToString(Span);

    public override TextSpan Span
    {
        get
        {
            var firstSignificantNodeOrToken = ChildNodesAndTokens
                .FirstOrDefault(n => n.IsSignificant);

            var lastSignificantNodeOrToken = ChildNodesAndTokens
                .LastOrDefault(n => n.IsSignificant);

            var startOfNonTriviaAndComments =
                firstSignificantNodeOrToken?.Span.Start ??
                FullSpan.Start;

            var endOfNonTriviaAndComments =
                lastSignificantNodeOrToken?.Span.End ??
                FullSpan.End;

            return TextSpan.FromBounds(startOfNonTriviaAndComments, endOfNonTriviaAndComments);
        }
    }
    
    /// <summary>
    /// Gets the text of the current node, including trivia.
    /// </summary>
    public string FullText => SourceText.ToString(FullSpan);

    public bool Contains(HttpSyntaxNode node) => false;

    public HttpSyntaxNode? FindNode(TextSpan span) =>
        DescendantNodesAndTokensAndSelf()
            .OfType<HttpSyntaxNode>()
            .Reverse()
            .FirstOrDefault(n => n.FullSpan.Contains(span));

    public HttpSyntaxNode? FindNode(int position) =>
        FindToken(position)?.Parent;

    public HttpSyntaxToken? FindToken(int position)
    {
        var candidate = _childNodesAndTokens.FirstOrDefault(n => n.FullSpan.Contains(position));

        return candidate switch
        {
            HttpSyntaxNode node => node.FindToken(position),
            HttpSyntaxToken token => token,
            _ => null
        };
    }

    private void GrowSpan(HttpSyntaxNodeOrToken child)
    {
        if (_fullSpan == default)
        {
            _fullSpan = child.FullSpan;
        }
        else
        {
            var _spanStart = Math.Min(_fullSpan.Start, child.FullSpan.Start);
            var _spanEnd = Math.Max(_fullSpan.End, child.FullSpan.End);
            _fullSpan = new TextSpan(_spanStart, _spanEnd - _fullSpan.Start);
        }
    }

    internal void Add(HttpSyntaxNodeOrToken child)
    {
        if (child.Parent is not null)
        {
            throw new InvalidOperationException($"{child.GetType().Name} {child} is already parented to {child.Parent}");
        }

        child.Parent = this;

        GrowSpan(child);

        _childNodesAndTokens.Add(child);
    }

    public IEnumerable<HttpSyntaxNode> ChildNodes =>
        _childNodesAndTokens.OfType<HttpSyntaxNode>();

    public IEnumerable<HttpSyntaxToken> ChildTokens => _childNodesAndTokens.OfType<HttpSyntaxToken>();

    public IReadOnlyList<HttpSyntaxNodeOrToken> ChildNodesAndTokens => _childNodesAndTokens;

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var child in ChildNodesAndTokens)
        {
            foreach (var diagnostic in child.GetDiagnostics())
            {
                yield return diagnostic;
            }
        }

        if (_diagnostics is not null)
        {
            foreach (var diagnostic in _diagnostics)
            {
                yield return diagnostic;
            }
        }
    }

    public IEnumerable<HttpSyntaxNodeOrToken> DescendantNodesAndTokensAndSelf()
    {
        yield return this;

        foreach (var syntaxNodeOrToken1 in DescendantNodesAndTokens())
        {
            yield return syntaxNodeOrToken1;
        }
    }

    public IEnumerable<HttpSyntaxNodeOrToken> DescendantNodesAndTokens() =>
        FlattenBreadthFirst(_childNodesAndTokens, n => n switch
        {
            HttpSyntaxNode node => node.ChildNodesAndTokens,
            _ => Array.Empty<HttpSyntaxNodeOrToken>()
        });

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