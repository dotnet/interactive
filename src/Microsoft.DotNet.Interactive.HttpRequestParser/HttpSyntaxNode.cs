// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal abstract class HttpSyntaxNode : HttpSyntaxNodeOrToken
{
    private TextSpan _fullSpan;
    private readonly List<HttpSyntaxNodeOrToken> _childNodesAndTokens = new();
    private TextSpan _span;
    private bool _isSignificant = false;

    private protected HttpSyntaxNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override TextSpan FullSpan => _fullSpan;

    public override bool IsSignificant => _isSignificant;

    public override TextSpan Span => _span;

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
            _span = child.Span;
        }
        else
        {
            // if the child span is empty and uninitialized, then set it to the end of the current node's span.
            if (child.FullSpan is { Start: 0, End: 0 })
            {
                if (child is HttpSyntaxNode childNode)
                {
                    childNode._fullSpan = new TextSpan(FullSpan.End, 0);
                }
            }

            var fullSpanStart = Math.Min(_fullSpan.Start, child.FullSpan.Start);
            var fullSpanEnd = Math.Max(_fullSpan.End, child.FullSpan.End);
            _fullSpan = new TextSpan(fullSpanStart, fullSpanEnd - _fullSpan.Start);

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

    internal void Add(HttpSyntaxToken token) => AddInternal(token);

    internal void Add(HttpCommentNode node) => AddInternal(node);

    protected void AddInternal(HttpSyntaxNodeOrToken child)
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

        _childNodesAndTokens.Add(child);

        GrowSpan(child);
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

        foreach (var node in DescendantNodesAndTokens())
        {
            yield return node;
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

    protected HttpBindingResult<string> BindByInterpolation(HttpBindingDelegate bind)
    {
        var text = new StringBuilder();
        var diagnostics = new List<Diagnostic>();
        var success = true;

        foreach (var node in ChildNodesAndTokens)
        {
            if (node is HttpEmbeddedExpressionNode { ExpressionNode: not null } n)
            {
                var innerResult = bind(n.ExpressionNode);

                if (innerResult.IsSuccessful)
                {
                    var nodeText = innerResult.Value?.ToString();
                    text.Append(nodeText);
                }
                else
                {
                    success = false;
                }

                diagnostics.AddRange(innerResult.Diagnostics);
            }
            else
            {
                text.Append(node.Text);
            }
        }

        if (success)
        {
            return HttpBindingResult<string>.Success(text.ToString().Trim());
        }
        else
        {
            return HttpBindingResult<string>.Failure(diagnostics.ToArray());
        }
    }
}