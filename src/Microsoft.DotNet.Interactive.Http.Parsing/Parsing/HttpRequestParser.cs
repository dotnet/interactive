// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using LinePosition = CodeAnalysis.Text.LinePosition;
using LinePositionSpan = CodeAnalysis.Text.LinePositionSpan;

internal class HttpRequestParser
{
    public static HttpRequestParseResult Parse(string code)
    {
        var parser = new HttpSyntaxParser(SourceText.From(code));

        var tree = parser.Parse();

        return new HttpRequestParseResult(tree);
    }

    private class HttpSyntaxParser
    {
        private readonly SourceText _sourceText;
        private IReadOnlyList<HttpSyntaxToken>? _tokens;
        private int _currentTokenIndex = 0;
        private readonly HttpSyntaxTree _syntaxTree;

        public HttpSyntaxParser(SourceText sourceText)
        {
            _sourceText = sourceText;
            _syntaxTree = new HttpSyntaxTree(_sourceText);
        }

        public HttpSyntaxTree Parse()
        {
            _tokens = new HttpLexer(_sourceText, _syntaxTree).Lex();

            var commentsToPrepend = new List<HttpCommentNode>();

            while (MoreTokens())
            {
                commentsToPrepend.AddRange(ParseComments());

                if (ParseVariableDeclarations() is { } variableNodes)
                {
                    foreach (var variableNode in variableNodes)
                    {
                        foreach (var comment in commentsToPrepend)
                        {
                            variableNode.Add(comment, addBefore: true);
                        }
                        commentsToPrepend.Clear();
                        _syntaxTree.RootNode.Add(variableNode);
                    }
                }

                if (ParseRequest() is { } requestNode)
                {
                    foreach (var comment in commentsToPrepend)
                    {
                        requestNode.Add(comment, addBefore: true);
                    }
                    commentsToPrepend.Clear();
                    _syntaxTree.RootNode.Add(requestNode);
                }

                if (ParseRequestSeparator() is { } separatorNode)
                {
                    _syntaxTree.RootNode.Add(separatorNode);
                }
            }

            return _syntaxTree;
        }

        private static void AddCommentsIfAny(
            List<HttpCommentNode> comments,
            HttpSyntaxNode toNode,
            bool addBefore = true)
        {
            foreach (var comment in comments)
            {
                toNode.Add(comment, addBefore: addBefore);
            }

            comments.Clear();
        }

        private IEnumerable<HttpVariableDeclarationAndAssignmentNode>? ParseVariableDeclarations()
        {
            while (MoreTokens() &&
                   !IsRequestSeparator())
            {
                if (GetNextSignificantToken() is { Kind: HttpTokenKind.Punctuation } and { Text: "@" })
                {
                    var variableNode = new HttpVariableDeclarationAndAssignmentNode(_sourceText, _syntaxTree);

                    variableNode.Add(ParseVariableDeclaration());
                    variableNode.Add(ParserVariableAssignment());
                    if (ParseVariableValue() is { } valueNode)
                    {
                        variableNode.Add(valueNode);
                        yield return variableNode;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private HttpVariableValueNode? ParseVariableValue()
        {
            HttpVariableValueNode? node = null;

            while (MoreTokens() &&
                CurrentToken is not { Kind: HttpTokenKind.NewLine })
            {
                if (node is null)
                {
                    if (CurrentToken is { Kind: HttpTokenKind.Word })
                    {
                        node = new HttpVariableValueNode(_sourceText, _syntaxTree);

                        ParseLeadingWhitespaceAndComments(node);
                    } 
                    else if (IsAtStartOfEmbeddedExpression())
                    {
                        node = new HttpVariableValueNode(_sourceText, _syntaxTree);
                        node.Add(ParseEmbeddedExpression());
                        if(CurrentToken is { Kind: HttpTokenKind.NewLine })
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (IsAtStartOfEmbeddedExpression())
                {
                    node.Add(ParseEmbeddedExpression());
                }
                else
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return node is not null
                       ? ParseTrailingWhitespace(node, stopAfterNewLine: true)
                       : null;
        }

        private HttpVariableAssignmentNode ParserVariableAssignment()
        {
            var node = new HttpVariableAssignmentNode(_sourceText, _syntaxTree);

            ParseLeadingWhitespaceAndComments(node);

            if (MoreTokens() && CurrentToken is { Text: "=" })
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingWhitespace(node);
        }

        private HttpVariableDeclarationNode ParseVariableDeclaration()
        {
            var node = new HttpVariableDeclarationNode(_sourceText, _syntaxTree);

            if (MoreTokens())
            {
                ParseLeadingWhitespaceAndComments(node);

                while (MoreTokens())
                {
                    if (CurrentToken is { Text: "@" })
                    {
                        ConsumeCurrentTokenInto(node);
                    }
                    else if (CurrentToken is { Kind: HttpTokenKind.Word })
                    {
                        ConsumeCurrentTokenInto(node);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return ParseTrailingWhitespace(node);
        }

        private HttpSyntaxToken? CurrentToken =>
            MoreTokens()
                ? _tokens![_currentTokenIndex]
                : null;

        private HttpSyntaxToken? CurrentTokenPlus(int offset)
        {
            var nextTokenIndex = _currentTokenIndex + offset;

            if (nextTokenIndex >= _tokens!.Count)
            {
                return null;
            }
            else
            {
                return _tokens![nextTokenIndex];
            }
        }

        [DebuggerStepThrough]
        private bool MoreTokens() => _tokens!.Count > _currentTokenIndex;

        [DebuggerStepThrough]
        private void AdvanceToNextToken() => _currentTokenIndex++;

        [DebuggerStepThrough]
        private void ConsumeCurrentTokenInto(HttpSyntaxNode node)
        {
            if (CurrentToken is { } token)
            {
                node.Add(token);
                AdvanceToNextToken();
            }
        }

        private T ParseLeadingWhitespaceAndComments<T>(T node) where T : HttpSyntaxNode
        {
            while (MoreTokens())
            {
                if (CurrentToken?.Kind is HttpTokenKind.Whitespace)
                {
                    ConsumeCurrentTokenInto(node);
                }
                else if (CurrentToken?.Kind is HttpTokenKind.NewLine)
                {
                    ConsumeCurrentTokenInto(node);
                }
                else if (IsComment())
                {
                    foreach (var commentNode in ParseComments())
                    {
                        node.Add(commentNode, addBefore: true);
                    }
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        private T ParseTrailingWhitespace<T>(T node, bool stopAfterNewLine = false, bool stopBeforeNewLine = false) where T : HttpSyntaxNode
        {
            while (MoreTokens())
            {
                if (CurrentToken?.Kind is HttpTokenKind.NewLine)
                {
                    if (stopBeforeNewLine)
                    {
                        break;
                    }

                    if (stopAfterNewLine)
                    {
                        ConsumeCurrentTokenInto(node);
                        break;
                    }
                }

                if (CurrentToken is not { Kind: HttpTokenKind.Whitespace } and not { Kind: HttpTokenKind.NewLine })
                {
                    break;
                }

                ConsumeCurrentTokenInto(node);
            }

            return node;
        }

        private HttpRequestNode? ParseRequest()
        {
            if (IsComment())
            {
                return null;
            }

            if (IsRequestSeparator())
            {
                return null;
            }

            var requestNode = new HttpRequestNode(
                _sourceText,
                _syntaxTree);

            ParseLeadingWhitespaceAndComments(requestNode);

            var methodNode = ParseMethod();
            if (methodNode is not null)
            {
                requestNode.Add(methodNode);
            }

            var urlNode = ParseUrl();
            if (urlNode is not null)
            {
                requestNode.Add(urlNode);
            }
            else
            {
                var span = new TextSpan(start: requestNode.Span.End, length: 0);
                var linePositionSpan = GetLinePositionSpanFromStartAndEndIndices(
                    requestNode.SourceText,
                    requestNode.Span.End,
                    requestNode.Span.End);
                var location = Location.Create(filePath: string.Empty, span, linePositionSpan);

                var diagnostic = requestNode.CreateDiagnostic(HttpDiagnostics.MissingUrl(), location);
                requestNode.AddDiagnostic(diagnostic);
            }

            var versionNode = ParseVersion();
            if (versionNode is not null)
            {
                requestNode.Add(versionNode);
            }
            else
            {
                ParseTrailingWhitespace(requestNode, stopAfterNewLine: true);
            }

            var comments = new List<HttpCommentNode>();
            comments.AddRange(ParseComments());

            var headersNode = ParseHeaders();
            if (headersNode is not null)
            {
                AddCommentsIfAny(comments, headersNode);
                requestNode.Add(headersNode);
            }
            else
            {
                AddCommentsIfAny(comments, requestNode, addBefore: false);
            }

            foreach (var comment in ParseComments())
            {
                requestNode.Add(comment);
            }

            ParseTrailingWhitespace(requestNode);

            var bodyNode = ParseBody();
            if (bodyNode is not null)
            {
                requestNode.Add(bodyNode);
            }
            else
            {
                AddCommentsIfAny(comments, requestNode, addBefore: false);
            }

            ParseTrailingWhitespace(requestNode);

            return requestNode;
        }

        private HttpRequestSeparatorNode? ParseRequestSeparator()
        {
            if (IsRequestSeparator())
            {
                var node = new HttpRequestSeparatorNode(_sourceText, _syntaxTree);
                ParseLeadingWhitespaceAndComments(node);

                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);

                return ParseTrailingWhitespace(node);
            }

            return null;
        }

        private HttpMethodNode? ParseMethod()
        {
            HttpMethodNode? node = null;

            while (CurrentToken?.Kind is HttpTokenKind.Word &&
                   CurrentTokenPlus(1)?.Kind is HttpTokenKind.Whitespace)
            {
                node = new HttpMethodNode(_sourceText, _syntaxTree);

                ParseLeadingWhitespaceAndComments(node);

                var verb = CurrentToken.Text;
                if (verb.ToLower() is not ("get" or "post" or "patch" or "put" or "delete" or "head" or "options" or "trace"))
                {
                    var diagnostic = CurrentToken.CreateDiagnostic(HttpDiagnostics.UnrecognizedVerb(verb));

                    node.AddDiagnostic(diagnostic);
                }

                ConsumeCurrentTokenInto(node);

                ParseTrailingWhitespace(node, stopBeforeNewLine: true);
            }

            return node;
        }

        private HttpUrlNode? ParseUrl()
        {
            HttpUrlNode? node = null;

            while ((CurrentToken?.Kind is HttpTokenKind.Word or HttpTokenKind.Punctuation) || (IsAtStartOfEmbeddedExpression()))
            {
                if (node is null)
                {
                    if (CurrentToken is { Kind: HttpTokenKind.Word })
                    {
                        node = new HttpUrlNode(_sourceText, _syntaxTree);

                        ParseLeadingWhitespaceAndComments(node);
                    }
                    else if (IsAtStartOfEmbeddedExpression())
                    {
                        node = new HttpUrlNode(_sourceText, _syntaxTree);
                        node.Add(ParseEmbeddedExpression());
                    }
                    else
                    {
                        break;
                    }
                }

                if (IsAtStartOfEmbeddedExpression())
                {
                    node.Add(ParseEmbeddedExpression());
                }
                else
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return node is not null
                       ? ParseTrailingWhitespace(node, stopBeforeNewLine: true)
                       : null;
        }

        private HttpSyntaxToken? GetNextSignificantToken()
        {
            var token = CurrentToken;
            int i = 0;

            while (MoreTokens())
            {
                if (token?.IsSignificant == true)
                {
                    return token;
                }

                if (_currentTokenIndex + i < _tokens!.Count)
                {
                    i++;
                    if (_currentTokenIndex + i >= _tokens.Count)
                    {
                        return null;
                    }
                    token = _tokens![_currentTokenIndex + i];
                }
                else
                {
                    break;
                }
            }

            return null;
        }

        private bool IsAtStartOfEmbeddedExpression() =>
            CurrentToken is { Text: "{" } &&
            CurrentTokenPlus(1) is { Text: "{" };

        private HttpEmbeddedExpressionNode ParseEmbeddedExpression()
        {
            var node = new HttpEmbeddedExpressionNode(_sourceText, _syntaxTree);

            node.Add(ParseExpressionStart());
            node.Add(ParseExpression());
            var expressionEnd = ParseExpressionEnd();
            if (expressionEnd is not null)
            {
                node.Add(expressionEnd);
            }

            return node;
        }

        private HttpExpressionStartNode ParseExpressionStart()
        {
            var node = new HttpExpressionStartNode(_sourceText, _syntaxTree);

            ConsumeCurrentTokenInto(node); // parse the first {
            ConsumeCurrentTokenInto(node); // parse the second {

            return ParseTrailingWhitespace(node);
        }

        private HttpExpressionNode ParseExpression()
        {
            var node = new HttpExpressionNode(_sourceText, _syntaxTree);
            ParseLeadingWhitespaceAndComments(node);

            while (MoreTokens() &&
                   !(CurrentToken is { Text: "}" } &&
                     CurrentTokenPlus(1) is { Text: "}" }))
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingWhitespace(node);
        }

        private HttpExpressionEndNode? ParseExpressionEnd()
        {
            if (CurrentToken?.Text is "}" &&
                CurrentTokenPlus(1)?.Text is "}")
            {
                var node = new HttpExpressionEndNode(_sourceText, _syntaxTree);

                ConsumeCurrentTokenInto(node); // parse the first }
                ConsumeCurrentTokenInto(node); // parse the second }

                return node;
            }
            else
            {
                return null;
            }
        }

        private HttpVersionNode? ParseVersion()
        {
            if (CurrentToken?.Kind is HttpTokenKind.Word)
            {
                var node = new HttpVersionNode(_sourceText, _syntaxTree);

                ParseLeadingWhitespaceAndComments(node);
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && CurrentToken.Kind is not HttpTokenKind.NewLine &&
                       !IsRequestSeparator())
                {
                    ConsumeCurrentTokenInto(node);
                }

                return ParseTrailingWhitespace(node, stopAfterNewLine: true);
            }

            return null;
        }

        private HttpHeadersNode? ParseHeaders()
        {
            HttpHeadersNode? headersNode = null;

            while (CurrentToken is { Kind: HttpTokenKind.Word } or { Text: ":" })
            {
                headersNode ??= new HttpHeadersNode(_sourceText, _syntaxTree);

                headersNode.Add(ParseHeader());
            }

            return headersNode;
        }

        private HttpHeaderNode ParseHeader()
        {
            var headerNode = new HttpHeaderNode(_sourceText, _syntaxTree);

            headerNode.Add(ParseHeaderName());
            headerNode.Add(ParserHeaderSeparator());
            headerNode.Add(ParseHeaderValue());

            return headerNode;
        }

        private HttpHeaderNameNode ParseHeaderName()
        {
            var node = new HttpHeaderNameNode(_sourceText, _syntaxTree);

            if (MoreTokens() &&
                CurrentToken is not { Kind: HttpTokenKind.NewLine } and not { Kind: HttpTokenKind.Punctuation, Text: ":" })
            {
                ParseLeadingWhitespaceAndComments(node);

                if (MoreTokens())
                {
                    ConsumeCurrentTokenInto(node);

                    while (MoreTokens())
                    {
                        if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: ":" })
                        {
                            break;
                        }

                        ConsumeCurrentTokenInto(node);
                    }
                }
            }

            return ParseTrailingWhitespace(node);
        }

        private HttpHeaderSeparatorNode ParserHeaderSeparator()
        {
            var node = new HttpHeaderSeparatorNode(_sourceText, _syntaxTree);

            ParseLeadingWhitespaceAndComments(node);

            if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: ":" })
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingWhitespace(node);
        }

        private HttpHeaderValueNode ParseHeaderValue()
        {
            var node = new HttpHeaderValueNode(_sourceText, _syntaxTree);

            ParseLeadingWhitespaceAndComments(node);

            while (MoreTokens() && CurrentToken is not { Kind: HttpTokenKind.NewLine })
            {
                if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "{" } &&
                    CurrentTokenPlus(1) is { Kind: HttpTokenKind.Punctuation } and { Text: "{" })
                {
                    node.Add(ParseEmbeddedExpression());
                }
                else
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingWhitespace(node, stopAfterNewLine: true);
        }

        private HttpBodyNode? ParseBody()
        {
            if (!MoreTokens())
            {
                return null;
            }

            if (IsRequestSeparator())
            {
                return null;
            }

            var node = new HttpBodyNode(_sourceText, _syntaxTree);

            if (MoreTokens() &&
                CurrentToken is not { Kind: HttpTokenKind.Whitespace } and not { Kind: HttpTokenKind.NewLine } &&
                !IsRequestSeparator())
            {
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && !IsRequestSeparator())
                {
                    if (IsAtStartOfEmbeddedExpression())
                    {
                        node.Add(ParseEmbeddedExpression());
                    }
                    else
                    {
                        ConsumeCurrentTokenInto(node);
                    }
                }
            }

            ParseTrailingWhitespace(node);

            return node;
        }

        private IEnumerable<HttpCommentNode> ParseComments()
        {
            while (IsComment())
            {
                var commentNode = new HttpCommentNode(_sourceText, _syntaxTree);

                var commentStartNode = ParseCommentStart();

                if (commentStartNode is not null)
                {
                    commentNode.Add(commentStartNode);
                }

                var commentBodyNode = ParseCommentBody();
                if (commentBodyNode is not null)
                {
                    commentNode.Add(commentBodyNode);
                }

                yield return commentNode;
            }
        }

        private HttpCommentBodyNode? ParseCommentBody()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var node = new HttpCommentBodyNode(_sourceText, _syntaxTree);
            ParseLeadingWhitespaceAndComments(node);

            while (MoreTokens() &&
                   CurrentToken is not { Kind: HttpTokenKind.NewLine })
            {
                ConsumeCurrentTokenInto(node);
            }

            ParseTrailingWhitespace(node);

            return node;
        }

        private HttpCommentStartNode? ParseCommentStart()
        {
            if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" })
            {
                var node = new HttpCommentStartNode(_sourceText, _syntaxTree);
                ConsumeCurrentTokenInto(node);
                return ParseTrailingWhitespace(node);
            }

            if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" } &&
                CurrentTokenPlus(1) is { Kind: HttpTokenKind.Punctuation } and { Text: "/" })
            {
                var node = new HttpCommentStartNode(_sourceText, _syntaxTree);

                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);
                return ParseTrailingWhitespace(node);
            }

            return null;
        }

        private bool IsComment()
        {
            if (MoreTokens() && !IsRequestSeparator())
            {
                if (CurrentToken is { Text: "#" })
                {
                    return true;
                }
                else if (CurrentToken is { Text: "/" } &&
                         CurrentTokenPlus(1) is { Text: "/" })
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private bool IsRequestSeparator() =>
            CurrentToken is { Text: "#" } &&
            CurrentTokenPlus(1) is { Text: "#" } &&
            CurrentTokenPlus(2) is { Text: "#" };

        private static LinePosition GetLinePositionFromCursorOffset(SourceText code, int cursorOffset)
        {
            int line = 0;
            int character = 0;
            for (int i = 0; i < cursorOffset && i < code.Length; i++)
            {
                switch (code[i])
                {
                    case '\n':
                        line++;
                        character = 0;
                        break;
                    default:
                        character++;
                        break;
                }
            }

            return new LinePosition(line, character);
        }

        private static LinePositionSpan GetLinePositionSpanFromStartAndEndIndices(SourceText code, int startIndex, int endIndex)
        {
            var start = GetLinePositionFromCursorOffset(code, startIndex);
            var end = GetLinePositionFromCursorOffset(code, endIndex);
            return new LinePositionSpan(start, end);
        }
    }

    private class HttpLexer
    {
        private readonly SourceText _sourceText;
        private readonly HttpSyntaxTree _syntaxTree;
        private TextWindow? _textWindow;
        private readonly List<HttpSyntaxToken> _tokens = new();

        public HttpLexer(SourceText sourceText, HttpSyntaxTree syntaxTree)
        {
            _sourceText = sourceText;
            _syntaxTree = syntaxTree;
        }

        public IReadOnlyList<HttpSyntaxToken> Lex()
        {
            _textWindow = new TextWindow(0, _sourceText.Length);

            HttpTokenKind? previousTokenKind = null;

            char previousCharacter = default;

            while (More())
            {
                var currentCharacter = _sourceText[_textWindow.End];

                var currentTokenKind = currentCharacter switch
                {
                    ' ' or '\t' => HttpTokenKind.Whitespace,
                    '\n' or '\r' or '\v' => HttpTokenKind.NewLine,
                    _ => char.IsLetterOrDigit(currentCharacter)
                             ? HttpTokenKind.Word
                             : HttpTokenKind.Punctuation,
                };

                if (previousTokenKind is { } previousTokenKindValue)
                {
                    if (!IsCurrentTokenANewLinePrecededByACarriageReturn(
                            previousTokenKindValue,
                            previousCharacter,
                            currentTokenKind,
                            currentCharacter) &&
                        (previousTokenKind != currentTokenKind || currentTokenKind
                             is HttpTokenKind.NewLine ||
                         currentTokenKind is HttpTokenKind.Punctuation))
                    {
                        FlushToken(previousTokenKindValue);
                    }
                }

                previousTokenKind = currentTokenKind;

                previousCharacter = currentCharacter;

                _textWindow.Advance();
            }

            if (previousTokenKind is not null)
            {
                FlushToken(previousTokenKind.Value);
            }

            return _tokens;
        }

        private void FlushToken(HttpTokenKind kind)
        {
            if (_textWindow is null || _textWindow.IsEmpty)
            {
                return;
            }

            _tokens.Add(new HttpSyntaxToken(kind, _sourceText, _textWindow.Span, _syntaxTree));

            _textWindow = new TextWindow(_textWindow.End, _sourceText.Length);
        }

        private bool More()
        {
            if (_textWindow is null)
            {
                return false;
            }

            return _textWindow.End < _sourceText.Length;
        }

        private bool IsCurrentTokenANewLinePrecededByACarriageReturn(
            HttpTokenKind previousTokenKindValue,
            char previousCharacter,
            HttpTokenKind currentTokenKind,
            char currentCharacter) =>
            currentTokenKind is HttpTokenKind.NewLine &&
            previousTokenKindValue is HttpTokenKind.NewLine
            &&
            previousCharacter is '\r' && currentCharacter is '\n';
    }

    private class TextWindow
    {
        public TextWindow(int start, int limit)
        {
            Start = start;
            Limit = limit;
            End = start;
        }

        public int Start { get; }

        public int End { get; private set; }

        public int Limit { get; }

        public int Length => End - Start;

        public bool IsEmpty => Start == End;

        public void Advance()
        {
            End++;

#if DEBUG
            if (End > Limit)
            {
                throw new InvalidOperationException();
            }
#endif
        }

        public TextSpan Span => new(Start, Length);

        public override string ToString() => $"[{Start}..{End}]";
    }
}
