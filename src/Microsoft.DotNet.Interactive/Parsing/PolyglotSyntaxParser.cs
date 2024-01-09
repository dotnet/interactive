// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotSyntaxParser
{
    private readonly SourceText _sourceText;
    private readonly PolyglotParserConfiguration _configuration;
    private int _currentTokenIndex = 0;
    private IReadOnlyList<SyntaxToken>? _tokens;
    private readonly PolyglotSyntaxTree _syntaxTree;
    private string? _currentKernelName;

    public static PolyglotSyntaxTree Parse(
        string code,
        string defaultLanguage,
        PolyglotParserConfiguration configuration)
    {
        var parser = new PolyglotSyntaxParser(SourceText.From(code), defaultLanguage, configuration);

        var tree = parser.Parse();

        return tree;
    }

    internal PolyglotSyntaxParser(
        SourceText sourceText,
        string? defaultLanguage,
        PolyglotParserConfiguration configuration)
    {
        DefaultKernelName = defaultLanguage ?? "";
        _sourceText = sourceText;
        _configuration = configuration;
        _syntaxTree = new(_sourceText, DefaultKernelName);
    }

    public string DefaultKernelName { get; }

    public PolyglotSyntaxTree Parse()
    {
        _currentKernelName = DefaultKernelName;
        _tokens = new PolyglotLexer(_sourceText, _syntaxTree).Lex();

        List<TopLevelSyntaxNode> accumulated = new();

        while (MoreTokens())
        {
            if (ParseDirective() is { } directiveNode)
            {
                if (_configuration.IsDirectiveInScope(_currentKernelName, directiveNode.DirectiveNameNode!.Text, out var kind))
                {
                    if (!IsCompilerDirective(directiveNode))
                    {
                        directiveNode.Kind = kind.Value;
                    }

                    if (directiveNode is { Kind: DirectiveNodeKind.KernelSelector })
                    {
                        _currentKernelName = directiveNode.ChildNodes
                                                          .OfType<DirectiveNameNode>()
                                                          .Single()
                                                          .ChildTokens
                                                          .First(t => t is { Kind: TokenKind.Word })
                                                          .Text;
                    }

                    _syntaxTree.RootNode.Add(directiveNode);
                }
                else
                {
                    accumulated.Add(directiveNode);
                }
            }
            else if (ParseLanguageNode() is { } languageNode)
            {
                if (accumulated.Count > 0)
                {
                    foreach (var node in Enumerable.Reverse(accumulated))
                    {
                        languageNode.Add(node, addBefore: true);
                    }

                    accumulated.Clear();
                }

                _syntaxTree.RootNode.Add(languageNode);
            }
        }

        if (accumulated.Count > 0)
        {
            foreach (var node in Enumerable.Reverse(accumulated))
            {
                _syntaxTree.RootNode.Add(node);
            }
        }

        return _syntaxTree;
    }

    private DirectiveNode? ParseDirective()
    {
        if (IsAtStartOfDirective())
        {
            var directiveNode = new DirectiveNode(_currentKernelName!, _sourceText, _syntaxTree);

            var directiveNameNode = ParseDirectiveName();

            directiveNode.Add(directiveNameNode);

            var consumeTrailingWhitespace = false;

            while (MoreTokens())
            {
                if (CurrentToken is null)
                {
                    break;
                }

                if (CurrentToken is { Kind: TokenKind.NewLine })
                {
                    ConsumeCurrentTokenInto(directiveNode);
                    consumeTrailingWhitespace = false;
                    break;
                }

                if (IsAtStartOfOption())
                {
                    if (ParseDirectiveOption() is { } optionNode)
                    {
                        directiveNode.Add(optionNode);
                    }
                }
                else
                {
                    if (ParseDirectiveArgument() is { } argumentNode)
                    {
                        directiveNode.Add(argumentNode);
                    }
                }
            }

            // certain directives might be compiler directives, e.g. #r and #i
            if (IsCompilerDirective(directiveNode))
            {
                directiveNode.Kind = DirectiveNodeKind.CompilerDirective;
            }

            if (consumeTrailingWhitespace)
            {
                return ParseTrailingWhitespace(directiveNode, true);
            }
            else
            {
                return directiveNode;
            }
        }

        return null;

        DirectiveNameNode ParseDirectiveName()
        {
            var directiveNameNode = new DirectiveNameNode(_sourceText, _syntaxTree);

            while (CurrentToken is { Kind: TokenKind.Punctuation or TokenKind.Word })
            {
                ConsumeCurrentTokenInto(directiveNameNode);
            }

            return ParseTrailingWhitespace(directiveNameNode, stopBeforeNewLine: true);
        }

        DirectiveArgumentNode? ParseDirectiveArgument()
        {
            var withinQuotes = false;

            DirectiveArgumentNode? argumentNode = null;

            while (MoreTokens())
            {
                if (CurrentToken is { Kind: TokenKind.Punctuation, Text: "\"" })
                {
                    withinQuotes = !withinQuotes;
                }

                if (CurrentToken is { Kind: TokenKind.NewLine } ||
                    CurrentToken is { Kind: TokenKind.Whitespace } && !withinQuotes)
                {
                    break;
                }

                argumentNode ??= new DirectiveArgumentNode(_sourceText, _syntaxTree);

                ConsumeCurrentTokenInto(argumentNode);
            }

            if (argumentNode is not null)
            {
                return ParseTrailingWhitespace(argumentNode, stopBeforeNewLine: true);
            }
            else
            {
                return null;
            }
        }

        DirectiveOptionNode? ParseDirectiveOption()
        {
            DirectiveOptionNode? optionNode = null;
            DirectiveOptionNameNode? optionNameNode = null;

            if (CurrentToken is { Kind: TokenKind.Punctuation } and { Text: "-" } ||
                CurrentTokenPlus(-1) is { Kind: TokenKind.Whitespace })
            {
                while (MoreTokens())
                {
                    if (CurrentToken is { Kind: TokenKind.NewLine } or { Kind: TokenKind.Whitespace })
                    {
                        break;
                    }

                    if (optionNode is null)
                    {
                        optionNode = new DirectiveOptionNode(_sourceText, _syntaxTree);
                        optionNameNode = new DirectiveOptionNameNode(_sourceText, _syntaxTree);
                    }

                    ConsumeCurrentTokenInto(optionNameNode!);
                }

                if (optionNode is not null && 
                    optionNameNode is not null)
                {
                    ParseTrailingWhitespace(optionNameNode, stopBeforeNewLine: true);

                    optionNode.Add(optionNameNode);

                    if (ParseDirectiveArgument() is { } argNode)
                    {
                        optionNode.Add(argNode);
                    }
                }
            }

            if (optionNode is not null)
            {
                return ParseTrailingWhitespace(optionNode, stopBeforeNewLine: true);
            }
            else
            {
                return null;
            }
        }
    }

    private bool IsAtStartOfOption()
    {
        return CurrentTokenPlus(-1) is { Kind: TokenKind.Whitespace } &&
               CurrentToken is { Kind: TokenKind.Punctuation } and { Text: "-" };
    }

    private LanguageNode ParseLanguageNode()
    {
        var node = new LanguageNode(_currentKernelName!, _sourceText, _syntaxTree);

        while (MoreTokens())
        {
            if (IsAtStartOfDirective())
            {
                break;
            }

            ConsumeCurrentTokenInto(node);
        }

        return node;
    }

    private T ParseTrailingWhitespace<T>(T node, bool stopAfterNewLine = false, bool stopBeforeNewLine = false) where T : SyntaxNode
    {
        while (MoreTokens())
        {
            if (CurrentToken?.Kind is TokenKind.NewLine)
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

            if (CurrentToken is not { Kind: TokenKind.Whitespace } and not { Kind: TokenKind.NewLine })
            {
                break;
            }

            ConsumeCurrentTokenInto(node);
        }

        return node;
    }

    private static bool IsCompilerDirective(DirectiveNode node) =>
        node.ChildNodes.OfType<DirectiveNameNode>().Any(n => n is { Text: "#r" or "#i" });

    private bool IsAtStartOfDirective()
    {
        if (CurrentToken is not { Text: "#" })
        {
            return false;
        }

        var previousToken = CurrentTokenPlus(-1);

        if (previousToken is not null && previousToken is not { Kind: TokenKind.NewLine })
        {
            return false;
        }

        return CurrentTokenPlus(1) is { Text: "!" } or { Text: "r" } or { Text: "i" };
    }

    private SyntaxToken? CurrentToken =>
        MoreTokens()
            ? _tokens![_currentTokenIndex]
            : null;

    private SyntaxToken? CurrentTokenPlus(int offset)
    {
        var nextTokenIndex = _currentTokenIndex + offset;

        if (nextTokenIndex >= _tokens!.Count)
        {
            return null;
        }
        else if (nextTokenIndex < 0)
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
    private void ConsumeCurrentTokenInto(SyntaxNode node)
    {
        if (CurrentToken is { } token)
        {
            node.Add(token);
            AdvanceToNextToken();
        }
    }
    
    private bool AllowsValueSharingByInterpolation(DirectiveNode directiveNode) =>
        directiveNode.Text != "set";

    internal class PolyglotLexer
    {
        private TextWindow? _textWindow;
        private readonly SourceText _sourceText;
        private readonly PolyglotSyntaxTree _syntaxTree;
        private readonly List<SyntaxToken> _tokens = new();

        public PolyglotLexer(SourceText sourceText, PolyglotSyntaxTree syntaxTree)
        {
            _sourceText = sourceText;
            _syntaxTree = syntaxTree;
        }

        public IReadOnlyList<SyntaxToken> Lex()
        {
            _textWindow = new TextWindow(0, _sourceText.Length);

            TokenKind? previousTokenKind = null;

            char previousCharacter = default;

            while (More())
            {
                var currentCharacter = _sourceText[_textWindow.End];

                var currentTokenKind = currentCharacter switch
                {
                    ' ' or '\t' => TokenKind.Whitespace,
                    '\n' or '\r' or '\v' => TokenKind.NewLine,
                    _ => char.IsLetterOrDigit(currentCharacter)
                             ? TokenKind.Word
                             : TokenKind.Punctuation,
                };

                if (previousTokenKind is { } previousTokenKindValue)
                {
                    if (previousTokenKind != currentTokenKind ||
                        currentTokenKind is TokenKind.NewLine ||
                        currentTokenKind is TokenKind.Punctuation)
                    {
                        if (!IsCurrentTokenANewLinePrecededByACarriageReturn(
                                previousTokenKindValue,
                                previousCharacter,
                                currentTokenKind,
                                currentCharacter))
                        {
                            FlushToken(previousTokenKindValue);
                        }
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

        private bool IsCurrentTokenANewLinePrecededByACarriageReturn(
            TokenKind previousTokenKindValue,
            char previousCharacter,
            TokenKind currentTokenKind,
            char currentCharacter) =>
            currentTokenKind is TokenKind.NewLine &&
            previousTokenKindValue is TokenKind.NewLine
            &&
            previousCharacter is '\r' && currentCharacter is '\n';

        private bool IsDirective()
        {
            if (GetNextChar() != '#')
            {
                return false;
            }

            switch (GetPreviousChar())
            {
                case default(char):
                case '\n':
                case '\r':
                    break;
                default:
                    return false;
            }

            // look ahead to see if this is a directive
            var textIsLongEnoughToContainDirective =
                _sourceText.Length >= _textWindow!.End + 2;

            if (!textIsLongEnoughToContainDirective)
            {
                return false;
            }

            if (!IsShebangAndNoFollowingWhitespace(_textWindow.End + 1, '!') &&
                !IsCharacterThenWhitespace('r') &&
                !IsCharacterThenWhitespace('i'))
            {
                return false;
            }

            return true;

            bool IsShebangAndNoFollowingWhitespace(int position, char value)
            {
                var next = position + 1;

                if (_sourceText[position] != value)
                {
                    return false;
                }

                if (_sourceText.Length <= next)
                {
                    return true;
                }

                return !char.IsWhiteSpace(_sourceText[next]);
            }

            bool IsCharacterThenWhitespace(char value)
            {
                var isChar = _sourceText[_textWindow.End + 1] == value;

                if (!isChar)
                {
                    return false;
                }

                var isFollowedByWhitespace = char.IsWhiteSpace(_sourceText[_textWindow.End + 2]);

                if (!isFollowedByWhitespace)
                {
                    return false;
                }

                return true;
            }
        }

        private void FlushToken(TokenKind kind)
        {
            if (_textWindow is null || _textWindow.IsEmpty)
            {
                return;
            }

            _tokens.Add(new SyntaxToken(kind, _sourceText, _textWindow.Span, _syntaxTree));

            _textWindow = new TextWindow(_textWindow.End, _sourceText.Length);
        }

        [DebuggerHidden]
        private char GetNextChar() => _sourceText[_textWindow!.End];

        [DebuggerHidden]
        private char GetPreviousChar() =>
            _textWindow!.End switch
            {
                0 => default,
                _ => _sourceText[_textWindow.End - 1]
            };

        [DebuggerHidden]
        private bool More()
        {
            return _textWindow!.End < _sourceText.Length;
        }

        private string CurrentTextWindow => _sourceText.GetSubText(_textWindow!.Span).ToString();

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
}