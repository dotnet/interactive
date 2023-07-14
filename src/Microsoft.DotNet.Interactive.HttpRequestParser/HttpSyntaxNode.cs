// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal abstract class HttpSyntaxNode : HttpSyntaxNodeOrToken
{
    private TextSpan _span;
    private readonly List<HttpSyntaxNodeOrToken> _childNodesAndTokens = new();

    private protected HttpSyntaxNode(
        string sourceText,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override TextSpan Span => _span;

    public bool Contains(HttpSyntaxNode node) => false;

    public HttpSyntaxNode? FindNode(TextSpan span) =>
        DescendantNodesAndTokensAndSelf()
            .OfType<HttpSyntaxNode>()
            .Reverse()
            .FirstOrDefault(n => n.Span.Contains(span));

    public HttpSyntaxNode? FindNode(int position) =>
        FindToken(position)?.Parent;

    public HttpSyntaxToken? FindToken(int position)
    {
        var candidate = _childNodesAndTokens.FirstOrDefault(n => n.Span.Contains(position));

        return candidate switch
        {
            HttpSyntaxNode node => node.FindToken(position),
            HttpSyntaxToken token => token,
            _ => null
        };
    }

    private void GrowSpan(HttpSyntaxNodeOrToken child)
    {
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

    public IEnumerable<HttpSyntaxNodeOrToken> ChildTokens => _childNodesAndTokens.OfType<HttpSyntaxToken>();

    public IReadOnlyList<HttpSyntaxNodeOrToken> ChildNodesAndTokens => _childNodesAndTokens;

    public IEnumerable<HttpSyntaxNodeOrToken> DescendantNodesAndTokensAndSelf()
    {
        yield return this;

        foreach (var syntaxNodeOrToken1 in DescendantNodesAndTokens())
        {
            yield return syntaxNodeOrToken1;
        }
    }

    public IEnumerable<HttpSyntaxNodeOrToken> DescendantNodesAndTokens() =>
        FlattenDepthFirst(_childNodesAndTokens, n => n switch
        {
            HttpSyntaxNode node => node.ChildNodesAndTokens,
            _ => Array.Empty<HttpSyntaxNodeOrToken>()
        });

    private static IEnumerable<T> FlattenDepthFirst<T>(
        IEnumerable<T> source,
        Func<T, IEnumerable<T>> children)
    {
        var stack = new Stack<T>();

        foreach (var item in source)
        {
            stack.Push(item);
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (var item in children(current))
            {
                stack.Push(item);
            }

            yield return current;
        }
    }
}
