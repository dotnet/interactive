// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class Lexer
    {
        private TextWindow _textWindow;
        private readonly SourceText _sourceText;
        private readonly List<SyntaxToken> _tokens = new List<SyntaxToken>();

        public Lexer(SourceText sourceText)
        {
            _sourceText = sourceText;
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

        [DebuggerHidden]
        private bool More()
        {
            return _textWindow.End < _sourceText.Length;
        }

        private void FlushToken(TokenKind kind)
        {
            if (_textWindow.IsEmpty)
            {
                return;
            }

            SyntaxToken token = kind switch
            {
                TokenKind.Language => new LanguageToken(_sourceText, _textWindow.Span),
                TokenKind.Directive => new DirectiveToken(_sourceText, _textWindow.Span),
                TokenKind.DirectiveArgs => new DirectiveArgsToken(_sourceText, _textWindow.Span),
                TokenKind.Trivia => new TriviaToken(_sourceText, _textWindow.Span),
                _ => throw new ArgumentOutOfRangeException()
            };

            _tokens.Add(token);

            _textWindow = new TextWindow(_textWindow.End, _sourceText.Length);
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

        private void LexDirective()
        {
            if (GetNextChar() != '#')
            {
                return;
            }

            // look ahead to see if this is a directive
            var textIsLongEnoughToContainDirective =
                _sourceText.Length >= _textWindow.End + 2;

            if (!textIsLongEnoughToContainDirective)
            {
                return;
            }

            if (!IsShebangAndNoFollowingWhitespace(_textWindow.End + 1, '!') &&
                !IsCharacterThenWhitespace('r') &&
                !IsCharacterThenWhitespace('i'))
            {
                return;
            }

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
                        return;

                    default:
                        _textWindow.Advance();
                        break;
                }
            }

            FlushToken(TokenKind.Directive);

            bool IsShebangAndNoFollowingWhitespace(int position, char value)
            {
                return _sourceText[position] == value &&
                       !char.IsWhiteSpace(_sourceText[position + 1]);
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
                        _textWindow.Advance();
                        break;

                    case '\n':
                        _textWindow.Advance();

                        if (foundArgs)
                        {
                            FlushToken(TokenKind.DirectiveArgs);
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
                LexDirective();

                if (More())
                {
                    _textWindow.Advance();
                }
            }

            FlushToken(TokenKind.Language);
        }

        [DebuggerHidden]
        private char GetNextChar() => _sourceText[_textWindow.End];

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