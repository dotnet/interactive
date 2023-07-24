// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

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

        public HttpSyntaxTree? Parse()
        {
            _tokens = new HttpLexer(_sourceText, _syntaxTree).Lex();
            if (_tokens.Count == 0)
            {
                return null;
            }

            var rootNode = new HttpRootSyntaxNode(
                _sourceText,
                _syntaxTree);

            _syntaxTree.RootNode = rootNode;

            while (MoreTokens())
            {
                // TODO ParseVariableDeclarations();

                if (ParseRequest() is { } requestNode)
                {
                    _syntaxTree.RootNode.Add(requestNode);
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
                else if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" })
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
            var methodNode = ParseMethod();
            var urlNode = ParseUrl();
            var versionNode = ParseVersion();
            var headersNode = ParseHeaders();
            var bodySeparatorNode = ParseBodySeparator();
            var bodyNode = ParseBody();

            return new HttpRequestNode(
                _sourceText,
                _syntaxTree,
                methodNode,
                urlNode,
                versionNode,
                headersNode,
                bodySeparatorNode,
                bodyNode);
        }

        private HttpMethodNode? ParseMethod()
        {
            var node = new HttpMethodNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Word)
            {
                if (CurrentToken.Kind is HttpTokenKind.Word && CurrentToken.Text.ToLower() is ("get" or "post" or "patch" or "put" or "delete" or "head" or "options" or "trace"))
                {                 
                    ConsumeCurrentTokenInto(node);
                }
                else if (CurrentToken.Kind is HttpTokenKind.Word && CurrentToken.Text.ToLower() is not ("get" or "post" or "patch" or "put" or "delete" or "head" or "options" or "trace" or "https"))
                {        
                    var tokenSpan = _sourceText.GetSubText(CurrentToken.Span).Lines.GetLinePositionSpan(CurrentToken.Span);                

                    var diagnostic = new Diagnostic(LinePositionSpan.FromCodeAnalysisLinePositionSpan(tokenSpan), DiagnosticSeverity.Warning, CurrentToken.Text.ToLower(), $"Unrecognized HTTP verb {CurrentToken.Text}");
                    node.AddDiagnostic(diagnostic);
                    ConsumeCurrentTokenInto(node);
                }
                else if (CurrentToken.Kind is HttpTokenKind.Word && CurrentToken.Text.ToLower() is "https")
                {
                    return null;
                }


            }
            else
            {
                /*var tokenSpan = _sourceText.GetSubText(CurrentToken.Span).Lines.GetLinePositionSpan(CurrentToken.Span);

                var diagnostic = new Diagnostic(LinePositionSpan.FromCodeAnalysisLinePositionSpan(tokenSpan), DiagnosticSeverity.Warning, CurrentToken.Text.ToLower(), $"Missing HTTP verb");
                node.AddDiagnostic(diagnostic);*/
            }

            return ParseTrailingTrivia(node);
        }

        private HttpUrlNode ParseUrl()
        {
            var node = new HttpUrlNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            while (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Word or HttpTokenKind.Punctuation)
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingTrivia(node, stopAfterNewLine: true);
        }

        private HttpVersionNode? ParseVersion()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var node = new HttpVersionNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Word)
            {
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && CurrentToken.Kind is not HttpTokenKind.NewLine)
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingTrivia(node, stopAfterNewLine: true);
        }

        private HttpHeadersNode? ParseHeaders()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var headerNodes = new List<HttpHeaderNode>();
            while (MoreTokens() && CurrentToken.Kind is not (HttpTokenKind.NewLine or HttpTokenKind.Whitespace))
            {
                headerNodes.Add(ParseHeader());
            }

            return new HttpHeadersNode(_sourceText, _syntaxTree, headerNodes);
        }

        private HttpHeaderNode ParseHeader()
        {
            var nameNode = ParseHeaderName();
            var separatorNode = ParserHeaderSeparator();
            var valueNode = ParseHeaderValue();

            return new HttpHeaderNode(_sourceText, _syntaxTree, nameNode, separatorNode, valueNode);
        }

        private HttpHeaderNameNode ParseHeaderName()
        {
            var node = new HttpHeaderNameNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Word)
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

            if (MoreTokens() && CurrentToken.Kind is not HttpTokenKind.NewLine)
            {
                ConsumeCurrentTokenInto(node);

                while (MoreTokens())
                {
                    if (CurrentToken.Kind is HttpTokenKind.NewLine)
                    {
                        break;
                    }

                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingTrivia(node, stopAfterNewLine: true);
        }

        private HttpBodySeparatorNode? ParseBodySeparator()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var node = new HttpBodySeparatorNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken.Kind is HttpTokenKind.Whitespace or HttpTokenKind.NewLine)
            {
                ConsumeCurrentTokenInto(node);

                while (MoreTokens() && CurrentToken.Kind is (HttpTokenKind.Whitespace or HttpTokenKind.NewLine))
                {
                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingTrivia(node);
        }

        private HttpBodyNode? ParseBody()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var node = new HttpBodyNode(_sourceText, _syntaxTree);

            ParseLeadingTrivia(node);

            if (MoreTokens() && CurrentToken.Kind is not (HttpTokenKind.Whitespace or HttpTokenKind.NewLine))
            {
                ConsumeCurrentTokenInto(node);

                while (MoreTokens())
                {

                    ConsumeCurrentTokenInto(node);
                }
            }

            return ParseTrailingTrivia(node);
        }

        private HttpCommentNode ParseComment()
        {
            var commentStartNode = ParseCommentStart();
            var commentBodyNode = ParseCommentBody();

            return new HttpCommentNode(_sourceText, _syntaxTree, commentStartNode, commentBodyNode);
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

        private HttpCommentStartNode ParseCommentStart()
        {
            var node = new HttpCommentStartNode(_sourceText, _syntaxTree);

            if (MoreTokens() && CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "#" })
            {
                ConsumeCurrentTokenInto(node);
            } 
            else if (MoreTokens() && CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" } &&
                NextToken is { Kind: HttpTokenKind.Punctuation } and { Text: "/" })
            {
                ConsumeCurrentTokenInto(node);
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingTrivia(node);
        }

    }
}
