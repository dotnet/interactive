// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpLexer
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
                _ => Char.IsPunctuation(currentCharacter)
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
        {

        }
    }
}
