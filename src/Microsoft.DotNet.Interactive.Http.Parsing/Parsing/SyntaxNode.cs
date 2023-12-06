// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal abstract class SyntaxNode : SyntaxNodeOrToken
{
    private TextSpan _fullSpan;
    private readonly List<SyntaxNodeOrToken> _childNodesAndTokens = new();
    private TextSpan _span;
    private bool _isSignificant = false;

    private protected SyntaxNode(
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

    public bool Contains(SyntaxNode node) => false;

    public SyntaxNode? FindNode(TextSpan span) =>
        DescendantNodesAndTokensAndSelf()
            .OfType<SyntaxNode>()
            .Reverse()
            .FirstOrDefault(n => n.FullSpan.Contains(span));

    public SyntaxNode? FindNode(int position) =>
        FindToken(position)?.Parent;

    public SyntaxToken? FindToken(int position)
    {
        var candidate = _childNodesAndTokens.FirstOrDefault(n => n.FullSpan.Contains(position));

        return candidate switch
        {
            SyntaxNode node => node.FindToken(position),
            SyntaxToken token => token,
            _ => null
        };
    }

    protected bool TextContainsWhitespace()
    {
        // We ignore whitespace if it's the first or last token, OR ignore the first or last token if it's not whitespace.  For this reason, the first and last tokens aren't interesting.
        for (var i = 1; i < _childNodesAndTokens.Count - 1; i++)
        {
            var nodeOrToken = _childNodesAndTokens[i];
            if (nodeOrToken is SyntaxToken { Kind: HttpTokenKind.Whitespace })
            {
                if (nodeOrToken.Span.OverlapsWith(_span))
                {
                    return true;
                }
            }
        }

        return false;
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

    internal void Add(HttpCommentNode node, bool addBefore) => AddInternal(node, addBefore);

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
    }

    public IEnumerable<SyntaxNode> ChildNodes =>
        _childNodesAndTokens.OfType<SyntaxNode>();

    public IEnumerable<SyntaxToken> ChildTokens => _childNodesAndTokens.OfType<SyntaxToken>();

    public IReadOnlyList<SyntaxNodeOrToken> ChildNodesAndTokens => _childNodesAndTokens;

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var child in ChildNodesAndTokens)
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
            return HttpBindingResult<string>.Success(text.ToString().Trim(), diagnostics.ToArray());
        }
        else
        {
            return HttpBindingResult<string>.Failure(diagnostics.ToArray());
        }
    }
}