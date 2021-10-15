﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class PolyglotLexer
    {
        private TextWindow _textWindow;
        private readonly SourceText _sourceText;
        private readonly PolyglotSyntaxTree _syntaxTree;
        private readonly List<SyntaxToken> _tokens = new List<SyntaxToken>();

        public PolyglotLexer(SourceText sourceText, PolyglotSyntaxTree syntaxTree)
        {
            _sourceText = sourceText;
            _syntaxTree = syntaxTree;
        }

        public IReadOnlyList<SyntaxToken> Lex()
        {
            _textWindow = new TextWindow(0, _sourceText.Length);

            while (More())
            {
                LexTrivia();

                LexSyntax();
            }

            return _tokens;
        }

        private void LexTrivia()
        {
            while (More())
            {
                switch (_sourceText[_textWindow.End])
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\v':
                        _textWindow.Advance();
                        break;

                    default:
                        FlushToken(TokenKind.Trivia);
                        return;
                }
            }
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

        private void LexDirective()
        {

            if (!_textWindow.IsEmpty)
            {
                FlushToken(TokenKind.Language);
            }

            while (More())
            {
                switch (GetNextChar())
                {
                    case ' ':
                    case '\t':
                        FlushToken(TokenKind.Directive);
                        LexDirectiveArgs();
                        return;

                    case '\r':
                    case '\n':
                        FlushToken(TokenKind.Directive);
                        LexTrivia();
                        return;

                    default:
                        _textWindow.Advance();
                        break;
                }
            }

            FlushToken(TokenKind.Directive);

        }

        private void LexDirectiveArgs()
        {
            var inTrivia = true;
            var foundArgs = false;

            while (More())
            {
                var next = GetNextChar();

                switch (next)
                {
                    case ' ' when inTrivia:
                    case '\t'  when inTrivia:
                        _textWindow.Advance();
                        break;

                    case '\r':
                    case '\n':
                        if (foundArgs)
                        {
                            FlushToken(TokenKind.DirectiveArgs);
                            LexTrivia();
                        }
                        else
                        {
                            FlushToken(TokenKind.Trivia);
                        }

                        return;

                    default:
                        if (inTrivia)
                        {
                            FlushToken(TokenKind.Trivia);
                            inTrivia = false;
                        }
                        else
                        {
                            _textWindow.Advance();
                        }

                        foundArgs = true;

                        break;
                }
            }

            FlushToken(TokenKind.DirectiveArgs);
        }

        private void LexSyntax()
        {
            while (More())
            {
                if (IsDirective())
                {
                    LexDirective();
                }
                else if(More())
                {
                    _textWindow.Advance();
                }
            }

            FlushToken(TokenKind.Language);
        }

        private void FlushToken(TokenKind kind)
        {
            if (_textWindow.IsEmpty)
            {
                return;
            }

            SyntaxToken token = kind switch
            {
                TokenKind.Language => new LanguageToken(_sourceText, _textWindow.Span, _syntaxTree),
                TokenKind.Directive => new DirectiveToken(_sourceText, _textWindow.Span, _syntaxTree),
                TokenKind.DirectiveArgs => new DirectiveArgsToken(_sourceText, _textWindow.Span, _syntaxTree),
                TokenKind.Trivia => new TriviaToken(_sourceText, _textWindow.Span, _syntaxTree),
                _ => throw new ArgumentOutOfRangeException()
            };

            _tokens.Add(token);

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

        private enum TokenKind
        {
            Language,
            Directive,
            DirectiveArgs,
            Trivia
        }
    }
}