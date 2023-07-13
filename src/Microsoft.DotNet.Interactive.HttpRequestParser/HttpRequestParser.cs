// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private IReadOnlyList<HttpSyntaxToken> _tokens;
        private int _currentTokenIndex = 0;
        private readonly HttpSyntaxTree _syntaxTree;

        public HttpSyntaxParser(string sourceText)
        {
            _sourceText = sourceText;
            _syntaxTree = new HttpSyntaxTree(_sourceText);
        }

        public HttpSyntaxTree Parse()
        {
            _tokens = new HttpLexer(_sourceText, _syntaxTree).Lex();

            var rootNode = new HttpRootSyntaxNode(
                _sourceText,
                _syntaxTree);

            _syntaxTree.RootNode = rootNode;

            while (MoreTokens())
            {
                ParseVariableDeclarations();

                if (ParseRequest() is { } requestNode)
                {
                    _syntaxTree.RootNode.Add(requestNode);
                }
            }

            return _syntaxTree;
        }

        private bool MoreTokens() => _tokens.Count > _currentTokenIndex;

        private void AdvanceToNextToken() => _currentTokenIndex++;

        private HttpSyntaxToken CurrentToken => _tokens[_currentTokenIndex];

        private void ParseVariableDeclarations()
        {
        }

        private HttpRequestNode ParseRequest()
        {
            var methodNode = ParseMethod();
            var urlNode = ParseUrl();
            var headersNode = ParseHeaders();
            var bodyNode = ParseBody();

            var requestNode = new HttpRequestNode(
                methodNode,
                urlNode,
                headersNode,
                bodyNode,
                _sourceText,
                _syntaxTree);

            return ParseTrailingTrivia(requestNode);
        }

        private HttpBodyNode ParseBody()
        {
            return null;
        }

        private HttpHeadersNode ParseHeaders()
        {
            return null;
        }

        private HttpUrlNode ParseUrl()
        {
            HttpUrlNode node = new(_sourceText, _syntaxTree);

            while (MoreTokens())
            {
                if (CurrentToken.Kind is HttpTokenKind.Word or HttpTokenKind.Punctuation)
                {
                    node.Add(CurrentToken);
                    AdvanceToNextToken();
                }
                else
                {
                    break;
                }
            }

            return ParseTrailingTrivia(node);
        }

        private HttpMethodNode ParseMethod()
        {
            var methodNode = new HttpMethodNode(_sourceText, _syntaxTree);

            var currentToken = CurrentToken;

            if (currentToken.Kind == HttpTokenKind.Word)
            {
                AdvanceToNextToken();

                methodNode.Add(currentToken);

                if (currentToken.Text.ToLower() is "get" or "post" or "patch" or "put" or "delete" or "head" or "options" or "trace")
                {
                    // TODO: (ParseMethod) add diagnostics
                }
            }

            return ParseTrailingTrivia(methodNode);
        }

        private T ParseTrailingTrivia<T>(T methodNode) where T : HttpSyntaxNode
        {
            while (MoreTokens())
            {
                if (CurrentToken.Kind is HttpTokenKind.Whitespace or HttpTokenKind.NewLine)
                {
                    methodNode.Add(CurrentToken);
                    AdvanceToNextToken();
                }
                else
                {
                    break;
                }
            }

            return methodNode;
        }
    }
}