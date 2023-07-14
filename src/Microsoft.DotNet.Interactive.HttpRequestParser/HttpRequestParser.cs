﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRequestParser
{
    public static HttpRequestParseResult Parse(string code)
    {
        var parser = new HttpSyntaxParser(code);

        var tree = parser.Parse();

        return new HttpRequestParseResult(tree);
    }

    private class HttpSyntaxParser
    {
        private readonly string _sourceText;
        private IReadOnlyList<HttpSyntaxToken>? _tokens;
        private int _currentTokenIndex = 0;
        private readonly HttpSyntaxTree _syntaxTree;

        public HttpSyntaxParser(string sourceText)
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

        private bool MoreTokens() => _tokens!.Count > _currentTokenIndex;

        private void AdvanceToNextToken() => _currentTokenIndex++;

        private void ConsumeCurrentTokenInto(HttpSyntaxNode node)
        {
            node.Add(CurrentToken);
            AdvanceToNextToken();
        }

        private T ParseTrailingTrivia<T>(T node) where T : HttpSyntaxNode
        {
            while (MoreTokens())
            {
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
            var bodyNode = ParseBody();

            return new HttpRequestNode(
                _sourceText,
                _syntaxTree,
                methodNode,
                urlNode,
                versionNode,
                headersNode,
                bodyNode);
        }

        private HttpMethodNode ParseMethod()
        {
            var node = new HttpMethodNode(_sourceText, _syntaxTree);

            if (CurrentToken.Kind is HttpTokenKind.Word)
            {
                ConsumeCurrentTokenInto(node);

                if (CurrentToken.Text.ToLower() is not ("get" or "post" or "patch" or "put" or "delete" or "head" or "options" or "trace"))
                {
                    // TODO: (ParseMethod) add diagnostics for unrecognized verb
                }
            }
            else
            {
                // TODO: (ParseMethod) add diagnostics for missing verb
            }

            return ParseTrailingTrivia(node);
        }

        private HttpUrlNode ParseUrl()
        {
            var node = new HttpUrlNode(_sourceText, _syntaxTree);

            while (MoreTokens())
            {
                if (CurrentToken.Kind is not (HttpTokenKind.Word or HttpTokenKind.Punctuation))
                {
                    break;
                }

                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingTrivia(node);
        }

        private HttpVersionNode? ParseVersion()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var node = new HttpVersionNode(_sourceText, _syntaxTree);

            if (CurrentToken.Kind is HttpTokenKind.Word)
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

            return ParseTrailingTrivia(node);
        }

        private HttpHeadersNode? ParseHeaders()
        {
            if (!MoreTokens())
            {
                return null;
            }

            var headerNodes = new List<HttpHeaderNode>();
            while (MoreTokens())
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

            if (CurrentToken.Kind is HttpTokenKind.Word)
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

            if (CurrentToken is { Kind: HttpTokenKind.Punctuation } and { Text: ":" })
            {
                ConsumeCurrentTokenInto(node);
            }

            return ParseTrailingTrivia(node);
        }

        private HttpHeaderValueNode ParseHeaderValue()
        {
            var node = new HttpHeaderValueNode(_sourceText, _syntaxTree);

            if (CurrentToken.Kind is not HttpTokenKind.NewLine)
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

            return ParseTrailingTrivia(node);
        }

        private HttpBodyNode? ParseBody()
        {
            return null;
        }
    }
}