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
    private string _currentKernelName;

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
                if (_configuration.IsDirectiveInScope(_currentKernelName, directiveNode.DirectiveName, out var kind))
                {
                    if (!IsCompilerDirective(directiveNode))
                    {
                        directiveNode.Kind = kind.Value;
                    }

                    if (directiveNode is { Kind: DirectiveNodeKind.KernelSelector })
                    {
                        _currentKernelName = directiveNode.ChildTokens.First(t => t.Kind == TokenKind.Word).Text;
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

    private LanguageNode ParseLanguageNode()
    {
        var node = new LanguageNode(_currentKernelName, _sourceText, _syntaxTree);

        while (MoreTokens())
        {
            if (IsDirective())
            {
                break;
            }

            ConsumeCurrentTokenInto(node);
        }

        return node;
    }

    private DirectiveNode ParseDirective()
    {
        if (IsDirective())
        {
            var directiveNode = new DirectiveNode(_currentKernelName, _sourceText, _syntaxTree);

            while (MoreTokens())
            {
                ConsumeCurrentTokenInto(directiveNode);

                if (CurrentToken is null)
                {
                    break;
                }

                if (CurrentToken is { Kind: TokenKind.NewLine })
                {
                    ConsumeCurrentTokenInto(directiveNode);
                    break;
                }
            }

            // certain directives might be compiler directives, e.g. #r and #i
            if (IsCompilerDirective(directiveNode))
            {
                directiveNode.Kind = DirectiveNodeKind.CompilerDirective;
            }

            return directiveNode;
        }

        return null;
    }

    private static bool IsCompilerDirective(DirectiveNode node) =>
        node.ChildNodesAndTokens.Count > 5 && 
        node.ChildTokens.ElementAt(1) is { Kind: TokenKind.Word } and { Text: "r" or "i" };

    private bool IsDirective()
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

    private void ParseSubmission(PolyglotSubmissionNode rootNode)
    {
        var currentKernelName = DefaultKernelName;
        // FIX: (ParseSubmission) rewrite

        // _subkernelInfoByKernelName.TryGetValue(currentKernelName ?? "", out var currentKernelInfo);

        for (var i = 0; i < _tokens!.Count; i++)
        {
            var currentToken = _tokens[i];

            // switch (currentToken)
            // {
            //     case DirectiveToken directiveToken:
            //
            //         DirectiveNode? directiveNode;
            //
            //         if (IsChooseKernelDirective(directiveToken))
            //         {
            //             directiveNode =
            //                 new KernelNameDirectiveNode(_sourceText, rootNode.SyntaxTree);
            //
            //             currentKernelName = directiveToken.DirectiveName;
            //
            //             if (_subkernelInfoByKernelName.TryGetValue(currentKernelName, out currentKernelInfo))
            //             {
            //                 directiveNode.CommandScope = currentKernelInfo.commandScope;
            //             }
            //         }
            //         else
            //         {
            //             directiveNode = new ActionDirectiveNode(
            //                 directiveToken,
            //                 _sourceText,
            //                 currentKernelName ?? DefaultLanguage,
            //                 rootNode.SyntaxTree);
            //
            //             directiveNode.AllowValueSharingByInterpolation = AllowsValueSharingByInterpolation(directiveToken);
            //         }
            //
            //         if (_tokens.Count > i + 1 && _tokens[i + 1] is TriviaToken triviaNode)
            //         {
            //             i += 1;
            //             directiveNode.Add(triviaNode);
            //         }
            //
            //         if (_tokens.Count > i + 1 &&
            //             _tokens[i + 1] is DirectiveArgsNode directiveArgs)
            //         {
            //             i += 1;
            //
            //             directiveNode.Add(directiveArgs);
            //         }
            //
            //         AssignDirectiveParser(directiveNode);
            //
            //         if (directiveToken.Text == "#r")
            //         {
            //             var parseResult = directiveNode.GetDirectiveParseResult();
            //             if (_subkernelInfoByKernelName.TryGetValue(currentKernelName ?? string.Empty,
            //                     out currentKernelInfo))
            //             {
            //                 directiveNode.CommandScope = currentKernelInfo.commandScope;
            //             }
            //
            //             if (parseResult.Errors.Count == 0)
            //             {
            //                 var value = parseResult.GetValueForArgument(parseResult.Parser.FindPackageArgument());
            //
            //                 if (value?.Value is FileInfo)
            //                 {
            //                     // #r <file> is treated as a LanguageNode to be handled by the compiler
            //                     AppendAsLanguageNode(directiveNode);
            //
            //                     break;
            //                 }
            //             }
            //         }
            //
            //         rootNode.Add(directiveNode);
            //
            //         break;
            //
            //     case LanguageToken languageToken:
            //         AppendAsLanguageNode(languageToken);
            //         break;
            //
            //     default:
            //         throw new ArgumentOutOfRangeException(nameof(currentToken));
            // }
        }

        void AppendAsLanguageNode(SyntaxNodeOrToken nodeOrToken)
        {
            // var previousSyntaxNode = rootNode.ChildNodes.LastOrDefault();
            // var previousLanguageNode = previousSyntaxNode as LanguageNode;
            // if (previousLanguageNode is { } &&
            //     previousLanguageNode is not KernelNameDirectiveNode &&
            //     previousLanguageNode.Name == currentKernelName)
            // {
            //     previousLanguageNode.Add(nodeOrToken);
            //     rootNode.GrowSpan(previousLanguageNode);
            // }
            // else
            // {
            //     var targetKernelName = currentKernelName ?? DefaultLanguage;
            //     var languageNode = new LanguageNode(
            //         _sourceText,
            //         rootNode.SyntaxTree);
            //     languageNode.CommandScope = currentKernelInfo.commandScope;
            //     languageNode.Add(nodeOrToken);
            //
            //     rootNode.Add(languageNode);
            // }
        }

        // void AssignDirectiveParser(DirectiveNode directiveNode)
        // {
        //     var directiveName = directiveNode.ChildNodesAndTokens[0].Text;
        //
        //     if (IsDefinedInRootKernel(directiveName))
        //     {
        //         directiveNode.DirectiveParser = _rootKernelDirectiveParser;
        //     }
        //     else if (_subkernelInfoByKernelName.TryGetValue(currentKernelName ?? string.Empty,
        //                                                     out var info))
        //     {
        //         directiveNode.DirectiveParser = info.getParser();
        //     }
        //     else
        //     {
        //         directiveNode.DirectiveParser = _rootKernelDirectiveParser;
        //     }
        // }
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
                    if (!IsCurrentTokenANewLinePrecededByACarriageReturn(
                            previousTokenKindValue,
                            previousCharacter,
                            currentTokenKind,
                            currentCharacter) &&
                        (previousTokenKind != currentTokenKind || currentTokenKind
                             is TokenKind.NewLine ||
                         currentTokenKind is TokenKind.Punctuation))
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

        private bool IsCurrentTokenANewLinePrecededByACarriageReturn(
            TokenKind previousTokenKindValue,
            char previousCharacter,
            TokenKind currentTokenKind,
            char currentCharacter) =>
            currentTokenKind is TokenKind.NewLine &&
            previousTokenKindValue is TokenKind.NewLine
            &&
            previousCharacter is '\r' && currentCharacter is '\n';

        private void LexTrivia()
        {
            // while (More())
            // {
            //     switch (_sourceText[_textWindow.End])
            //     {
            //         case ' ':
            //         case '\t':
            //         case '\r':
            //         case '\n':
            //         case '\v':
            //             _textWindow.Advance();
            //             break;
            //
            //         default:
            //             FlushToken(TokenKind.Trivia);
            //             return;
            //     }
            // }
        }

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
                _sourceText.Length >= _textWindow.End + 2;

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
        private char GetNextChar() => _sourceText[_textWindow.End];

        [DebuggerHidden]
        private char GetPreviousChar() =>
            _textWindow.End switch
            {
                0 => default,
                _ => _sourceText[_textWindow.End - 1]
            };

        [DebuggerHidden]
        private bool More()
        {
            return _textWindow.End < _sourceText.Length;
        }

        private string CurrentTextWindow => _sourceText.GetSubText(_textWindow.Span).ToString();

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

