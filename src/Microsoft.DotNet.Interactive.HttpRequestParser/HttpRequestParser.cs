// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.HttpRequest;

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

            while (MoreTokens())
            {
                // TODO ParseVariableDeclarations();

                if (ParseRequest() is { } requestNode)
                {
                    _syntaxTree.RootNode.Add(requestNode);
                }

                if (ParseRequestSeparator() is { } separatorNode)
                {
                    _syntaxTree.RootNode.Add(separatorNode);
                }
            }

            return _syntaxTree;
        }

        private HttpSyntaxToken CurrentToken => _tokens![_currentTokenIndex];

        private HttpSyntaxToken? NextToken
        {
            get
            {
                var nextTokenIndex = _currentTokenIndex + 1;
                return nextTokenIndex >= _tokens!.Count ? null : _tokens![nextTokenIndex];
            }
        }

        private HttpSyntaxToken? NextNextToken
        {
            get
            {
                var nextNextTokenIndex = _currentTokenIndex + 2;
                return nextNextTokenIndex >= _tokens!.Count ? null : _tokens![nextNextTokenIndex];
            }
        }

        [DebuggerStepThrough]
        private bool MoreTokens() => _tokens!.Count > _currentTokenIndex;

        private void AdvanceToNextToken() => _currentTokenIndex++;

        private void ConsumeCurrentTokenInto(HttpSyntaxNode node)
        {
            node.Add(CurrentToken);
            AdvanceToNextToken();
        }

        private T ParseLeadingTrivia<T>(T node) where T : HttpSyntaxNode
        {
            while (MoreTokens())
            {
                if (CurrentToken.Kind is HttpTokenKind.Whitespace)
                {
                    ConsumeCurrentTokenInto(node);
                }
                else if (CurrentToken.Kind is HttpTokenKind.NewLine)
                {
                    ConsumeCurrentTokenInto(node);
                }
                else if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" } &&
                    !(NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" } &&
                    NextNextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" }))
                {
                    node.Add(ParseComment());
                }
                else if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" } &&
                    NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" })
                {
                    node.Add(ParseComment());
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        private T ParseTrailingTrivia<T>(T node, bool stopAfterNewLine = false) where T : HttpSyntaxNode
        {
            while (MoreTokens())
            {
                if (stopAfterNewLine && CurrentToken.Kind is HttpTokenKind.NewLine)
                {
                    ConsumeCurrentTokenInto(node);
                    break;
                }

                if (CurrentToken.Kind is not (HttpTokenKind.Whitespace or HttpTokenKind.NewLine))
                {
                    break;
                }

                ConsumeCurrentTokenInto(node);
            }

            return node;
        }

        private HttpRequestNode ParseRequest()
        {
            var requestNode = new HttpRequestNode(
                _sourceText,
                _syntaxTree);

            ParseLeadingTrivia(requestNode);

            var methodNode = ParseMethod();
            if (methodNode is not null)
            {
                requestNode.Add(methodNode);
            }

            var urlNode = ParseUrl();
            requestNode.Add(urlNode);

            var versionNode = ParseVersion();
            if (versionNode is not null)
            {
                requestNode.Add(versionNode);
            }

            var headersNode = ParseHeaders();
            if (headersNode is not null)
            {
                requestNode.Add(headersNode);
            }

            var bodySeparatorNode = ParseBodySeparator();
            if (bodySeparatorNode is not null)
            {
                requestNode.Add(bodySeparatorNode);
            }

            var bodyNode = ParseBody();
            if (bodyNode is not null)
            {
                requestNode.Add(bodyNode);
            }

            return requestNode;
        }

        private HttpRequestSeparatorNode? ParseRequestSeparator()
        {
            if (MoreTokens() && IsRequestSeparator())
            {
                var node = new HttpRequestSeparatorNode(_sourceText, _syntaxTree);
                ParseLeadingTrivia(node);
                
                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && CurrentToken.Kind is not (HttpTokenKind.NewLine or HttpTokenKind.Whitespace))
                {
                    ConsumeCurrentTokenInto(node);
                }

                ParseTrailingTrivia(node);
            }

            return null;
        }

        private HttpMethodNode? ParseMethod()
        {
            HttpMethodNode? node = null;

            if (MoreTokens())
            {
                if (CurrentToken.Text.ToLower() is "http" or "https")
                {
                    return null;
                }
        
                if (CurrentToken.Kind is HttpTokenKind.Word)
                {
                    node = new HttpMethodNode(_sourceText, _syntaxTree);
        
                    ParseLeadingTrivia(node);
        
                    if (CurrentToken.Text.ToLower() is not ("get" or "post" or "patch" or "put" or "delete" or "head" or "options" or "trace"))
                    {
                        var message = $"Unrecognized HTTP verb {CurrentToken.Text}";
        
                        var diagnostic = CurrentToken.CreateDiagnostic(message);
        
                        node.AddDiagnostic(diagnostic);
                    }
        
                    ConsumeCurrentTokenInto(node);
                    ParseTrailingTrivia(node);
                }
            }
        
            return node;
        }

        private HttpUrlNode ParseUrl()
        {
            var node = new HttpUrlNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            while (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Word or HttpTokenKind.Punctuation)
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

            return ParseTrailingTrivia(node, stopAfterNewLine: true);
        }

        private bool IsAtStartOfEmbeddedExpression()
        {
            return CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "{" } &&
                   NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "{" };
        }

        private HttpEmbeddedExpressionNode ParseEmbeddedExpression()
        {
            var startNode = ParseExpressionStart();
            var expressionNode = ParseExpression();
            var endNode = ParseExpressionEnd();

            return new HttpEmbeddedExpressionNode(_sourceText, _syntaxTree, startNode, expressionNode, endNode);
        }

        private HttpExpressionStartNode ParseExpressionStart()
        {
            var node = new HttpExpressionStartNode(_sourceText, _syntaxTree);

            ConsumeCurrentTokenInto(node); // parse the first {
            ConsumeCurrentTokenInto(node); // parse the second {

            return ParseTrailingTrivia(node);
        }

        private HttpExpressionNode ParseExpression()
        {
            var node = new HttpExpressionNode(_sourceText, _syntaxTree);
            ParseLeadingTrivia(node);

            while (MoreTokens() && !(CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "}" } &&
                   NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "}" }))
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingTrivia(node);
        }

        private HttpExpressionEndNode ParseExpressionEnd()
        {
            var node = new HttpExpressionEndNode(_sourceText, _syntaxTree);

            ConsumeCurrentTokenInto(node); // parse the first }
            ConsumeCurrentTokenInto(node); // parse the second }

            return node;
        }

        private HttpVersionNode? ParseVersion()
        {
            if (MoreTokens() &&
                CurrentToken.Kind is HttpTokenKind.Word &&
                CurrentToken.Text.ToLowerInvariant() is "http" or "https")
            {
                var node = new HttpVersionNode(_sourceText, _syntaxTree);

                ParseLeadingTrivia(node);
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && CurrentToken.Kind is not HttpTokenKind.NewLine &&
                       !IsRequestSeparator())
                {
                    ConsumeCurrentTokenInto(node);
                }

                return ParseTrailingTrivia(node, stopAfterNewLine: true);
            }

            return null;
        }

        private HttpHeadersNode? ParseHeaders()
        {
            HttpHeadersNode? headersNode = null;

            while (MoreTokens() &&
                   CurrentToken.Kind is (HttpTokenKind.Word) &&
                   !IsRequestSeparator())
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
            if (MoreTokens())
            {
                var node = new HttpHeaderNameNode(_sourceText, _syntaxTree);

                ParseLeadingTrivia(node);

                if (MoreTokens())
                {
                    ConsumeCurrentTokenInto(node);

                    while (MoreTokens())
                    {
                        if (CurrentToken.Kind is HttpTokenKind.Whitespace or HttpTokenKind.NewLine)
                        {
                            break;
                        }

                        if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: ":" })
                        {
                            break;
                        }

                        ConsumeCurrentTokenInto(node);
                    }
                }

                return ParseTrailingTrivia(node);
            }

            return null;
        }

        private HttpHeaderSeparatorNode ParserHeaderSeparator()
        {
            var node = new HttpHeaderSeparatorNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: ":" })
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingTrivia(node);
        }

        private HttpHeaderValueNode ParseHeaderValue()
        {
            var node = new HttpHeaderValueNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            while (MoreTokens() && CurrentToken.Kind is not HttpTokenKind.NewLine)
            {
                if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "{" } &&
                    NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "{" })
                {
                    node.Add(ParseEmbeddedExpression());
                }
                else
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingTrivia(node, stopAfterNewLine: true);
        }

        private HttpBodySeparatorNode? ParseBodySeparator()
        {
            if (!MoreTokens() || IsRequestSeparator())
            {
                return null;
            }

            var node = new HttpBodySeparatorNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Whitespace or HttpTokenKind.NewLine &&
                !IsRequestSeparator())
            {
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && CurrentToken.Kind is (HttpTokenKind.Whitespace or HttpTokenKind.NewLine) &&
                    !IsRequestSeparator())
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingTrivia(node);
        }

        private HttpBodyNode? ParseBody()
        {
            if (!MoreTokens() || IsRequestSeparator())
            {
                return null;
            }

            var node = new HttpBodyNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() &&
                CurrentToken.Kind is not (HttpTokenKind.Whitespace or HttpTokenKind.NewLine) &&
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

            return ParseTrailingTrivia(node);
        }

        private HttpCommentNode ParseComment()
        {
            var commentNode = new HttpCommentNode(_sourceText, _syntaxTree);

            var commentStartNode = ParseCommentStart();

            if (commentStartNode is null)
            {
                return null;
            }

            commentNode.Add(commentStartNode);

            var commentBodyNode = ParseCommentBody();
            if (commentBodyNode is not null)
            {
                commentNode.Add(commentBodyNode);
            }

            return commentNode;
        }

        private HttpCommentBodyNode? ParseCommentBody()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var node = new HttpCommentBodyNode(_sourceText, _syntaxTree);
            ParseLeadingTrivia(node);

            while (MoreTokens() && CurrentToken.Kind is not HttpTokenKind.NewLine)
            {
                ConsumeCurrentTokenInto(node);
            }

            return node;
        }

        private HttpCommentStartNode? ParseCommentStart()
        {
            if (MoreTokens() && CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" })
            {
                var node = new HttpCommentStartNode(_sourceText, _syntaxTree);
                ConsumeCurrentTokenInto(node);
                return ParseTrailingTrivia(node);
            }

            if (MoreTokens() && CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" } &&
                NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" })
            {
                var node = new HttpCommentStartNode(_sourceText, _syntaxTree);

                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);
                return ParseTrailingTrivia(node);
            }

            return null;
        }

        private bool IsRequestSeparator()
        {
            return CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" } &&
                   (NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" } &&
                    NextNextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" });
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
                    _ => char.IsPunctuation(currentCharacter)
                             ? HttpTokenKind.Punctuation
                             : HttpTokenKind.Word,
                };

                if (previousTokenKind is { } previousTokenKindValue)
                {

                    if (!IsCurrentTokenANewLinePrecededByACarriageReturn(previousTokenKindValue, previousCharacter,
                        currentTokenKind, currentCharacter) && (previousTokenKind != currentTokenKind || currentTokenKind
                        is HttpTokenKind.NewLine || currentTokenKind is HttpTokenKind.Punctuation))
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

        private bool IsCurrentTokenANewLinePrecededByACarriageReturn(HttpTokenKind previousTokenKindValue, char previousCharacter, HttpTokenKind currentTokenKind, char currentCharacter)
        {
            return (currentTokenKind is HttpTokenKind.NewLine && previousTokenKindValue is HttpTokenKind.NewLine
                && previousCharacter is '\r' && currentCharacter is '\n');
        }
    }
}
