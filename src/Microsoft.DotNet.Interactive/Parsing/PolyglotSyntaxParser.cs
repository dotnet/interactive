// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotSyntaxParser
{
    private readonly SourceText _sourceText;
    private readonly PolyglotParserConfiguration _configuration;
    private int _currentTokenIndex = 0;
    private IReadOnlyList<SyntaxToken>? _tokens;
    private readonly PolyglotSyntaxTree _syntaxTree;
    private string _currentKernelName = "";
    private readonly KernelInfo? _compositeKernelInfo = null;

    public static PolyglotSyntaxTree Parse(
        string code,
        PolyglotParserConfiguration configuration)
    {
        var parser = new PolyglotSyntaxParser(SourceText.From(code), configuration);

        var tree = parser.Parse();

        return tree;
    }

    internal PolyglotSyntaxParser(
        SourceText sourceText,
        PolyglotParserConfiguration configuration)
    {
        _sourceText = sourceText;
        _configuration = configuration;
        _syntaxTree = new(_sourceText, configuration);
        _compositeKernelInfo = configuration.KernelInfos.FirstOrDefault(i => i.IsComposite);
    }

    public PolyglotSyntaxTree Parse()
    {
        _currentKernelName = _configuration.DefaultKernelName;
        _tokens = new PolyglotLexer(_sourceText, _syntaxTree).Lex();

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
                }

                _syntaxTree.RootNode.Add(directiveNode);
            }
            else if (ParseLanguageNode() is { } languageNode)
            {
                _syntaxTree.RootNode.Add(languageNode);
            }
        }

        return _syntaxTree;
    }

    private DirectiveNode? ParseDirective()
    {
        if (IsAtStartOfDirective())
        {
            KernelDirective? currentlyScopedDirective = null;

            var directiveNameNode = ParseParameterName();

            var targetKernelName = _currentKernelName;

            if (_configuration.TryGetDirectiveByName(_currentKernelName, directiveNameNode.Text, out var directive1) ||
                _compositeKernelInfo?.TryGetDirective(directiveNameNode.Text, out directive1) == true)
            {
                currentlyScopedDirective = directive1;

                if (directive1 is not KernelSpecifierDirective)
                {
                    if (directive1.ParentKernelInfo is { } parentKernelInfo)
                    {
                        targetKernelName = parentKernelInfo.LocalName;
                    }
                }
            }

            var directiveNode = new DirectiveNode(targetKernelName, _sourceText, _syntaxTree);

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

                if (IsAtStartOfParameterName())
                {
                    if (ParseNamedParameter() is { } namedParameterNode)
                    {
                        directiveNode.Add(namedParameterNode);

                        if (!_configuration.IsParameterInScope(namedParameterNode))
                        {
                            var diagnostic = namedParameterNode.CreateDiagnostic(
                                new(ErrorCodes.UnknownParameterName,
                                    "Unrecognized parameter name '{0}'",
                                    DiagnosticSeverity.Error,
                                    namedParameterNode.NameNode?.Text ?? ""));
                            namedParameterNode.AddDiagnostic(diagnostic);
                        }
                    }
                }
                else
                {
                    if (CurrentToken is { Kind: TokenKind.Word } word &&
                        currentlyScopedDirective is KernelActionDirective actionDirective &&
                        actionDirective.TryGetSubcommand(word.Text, out var subcommand))
                    {
                        currentlyScopedDirective = subcommand;
                        var subcommandNode = new DirectiveSubcommandNode(_sourceText, _syntaxTree);
                        ConsumeCurrentTokenInto(subcommandNode);
                        ParseTrailingWhitespace(subcommandNode);
                        directiveNode.Add(subcommandNode);
                    }
                    else if (ParseParameterValue() is { } parameterValueNode)
                    {
                        DirectiveParameterNode parameterNode = new(_sourceText, _syntaxTree);
                        parameterNode.Add(parameterValueNode);
                        directiveNode.Add(parameterNode);
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

        DirectiveNameNode ParseParameterName()
        {
            var directiveNameNode = new DirectiveNameNode(_sourceText, _syntaxTree);

            while (CurrentToken is { Kind: TokenKind.Punctuation or TokenKind.Word })
            {
                ConsumeCurrentTokenInto(directiveNameNode);
            }

            return ParseTrailingWhitespace(directiveNameNode, stopBeforeNewLine: true);
        }

        DirectiveParameterValueNode ParseParameterValue()
        {
            DirectiveParameterValueNode valueNode = new(_sourceText, _syntaxTree);

            if (CurrentToken is { Kind: TokenKind.Punctuation } and { Text: "{" or "\"" })
            {
                ParseJsonValueInto(valueNode);

                ParseTrailingWhitespace(valueNode, stopBeforeNewLine: true);
            }
            else if (CurrentToken is not ({ Kind: TokenKind.Punctuation } and { Text: "@" }))
            {
                ParsePlainTextInto(valueNode);

                ParseTrailingWhitespace(valueNode, stopBeforeNewLine: true);
            }
            else
            {
                var expressionNode = new DirectiveExpressionNode(_sourceText, _syntaxTree);

                var tokenNameNode = new DirectiveExpressionTypeNode(_sourceText, _syntaxTree);

                ConsumeCurrentTokenInto(tokenNameNode);

                if (CurrentToken is { Kind: TokenKind.Word })
                {
                    ConsumeCurrentTokenInto(tokenNameNode);
                }

                if (CurrentToken is { Kind: TokenKind.Punctuation } and { Text: ":" })
                {
                    ConsumeCurrentTokenInto(tokenNameNode);
                }

                expressionNode.Add(tokenNameNode);

                var inputParametersNode = new DirectiveExpressionParametersNode(_sourceText, _syntaxTree);

                if (CurrentToken is { Kind : TokenKind.Punctuation } and ({ Text: "{" } or { Text: "\"" }))
                {
                    ParseJsonValueInto(inputParametersNode);
                }
                else
                {
                    ParsePlainTextInto(inputParametersNode);
                }

                expressionNode.Add(inputParametersNode);

                valueNode.Add(expressionNode);

                ParseTrailingWhitespace(inputParametersNode, stopBeforeNewLine: true);
            }

            return valueNode;

            void ParsePlainTextInto(SyntaxNode node)
            {
                while (MoreTokens())
                {
                    if (CurrentToken is { Kind: TokenKind.NewLine } or { Kind: TokenKind.Whitespace })
                    {
                        break;
                    }
                    
                    ConsumeCurrentTokenInto(node);
                }
            }

            void ParseJsonValueInto(SyntaxNode node)
            {
                var currentToken = CurrentToken;

                if (currentToken is { Kind: TokenKind.Punctuation } and { Text: "{" })
                {
                    // Parse a JSON object
                    var jsonDepth = 0;

                    while (MoreTokens())
                    {
                        currentToken = CurrentToken;

                        if (currentToken is { Kind: TokenKind.NewLine })
                        {
                            break;
                        }

                        switch (currentToken)
                        {
                            case { Kind: TokenKind.Punctuation } and { Text: "{" }:
                                jsonDepth++;
                                break;

                            case { Kind: TokenKind.Punctuation } and { Text: "}" }:
                                jsonDepth--;
                                break;
                        }

                        ConsumeCurrentTokenInto(node);

                        if (jsonDepth <= 0)
                        {
                            break;
                        }
                    }
                }
                else if (currentToken is { Kind: TokenKind.Punctuation } and { Text: "\"" })
                {
                    // Parse a JSON string
                    var quoteCount = 0;

                    while (MoreTokens())
                    {
                        currentToken = CurrentToken;

                        if (currentToken is { Kind: TokenKind.NewLine })
                        {
                            break;
                        }

                        if (currentToken is { Kind: TokenKind.Punctuation } and { Text: "\"" })
                        {
                            if (CurrentTokenPlus(-1) is not { Text: "\\" })
                            {
                                quoteCount++;
                            }
                        }

                        ConsumeCurrentTokenInto(node);

                        if (quoteCount == 2)
                        {
                            break;
                        }
                    }
                }

                if (node.Text is { } json)
                {
                    try
                    {
                        JsonDocument.Parse(json);
                    }
                    catch (JsonException exception)
                    {
                        var positionInLine = (int)exception.BytePositionInLine! + node.FullSpan.Start;

                        var location = Location.Create(
                            filePath: string.Empty,
                            new TextSpan(positionInLine, 1),
                            new(new(0, positionInLine), new(0, positionInLine + 1)));

                        var message = exception.Message;

                        if (message.IndexOf(" LineNumber", StringComparison.InvariantCulture) is var index and > -1)
                        {
                            // Example message to be cleaned up since the character positions won't be accurate for the user's complete text: "Invalid JSON: 'c' is an invalid start of a value. LineNumber: 0 | BytePositionInLine: 11." 
                            message = message.Remove(index);
                        }

                        var diagnostic = node.CreateDiagnostic(
                            new(ErrorCodes.InvalidJsonInParameterValue,
                                "Invalid JSON: {0}",
                                DiagnosticSeverity.Error,
                                message),
                            location);

                        node.AddDiagnostic(diagnostic);
                    }
                }
            }
        }

        DirectiveParameterNode? ParseNamedParameter()
        {
            DirectiveParameterNode? parameterNode = null;
            DirectiveParameterNameNode? parameterNameNode = null;

            if (CurrentToken is { Kind: TokenKind.Punctuation } and { Text: "-" } ||
                CurrentTokenPlus(-1) is { Kind: TokenKind.Whitespace })
            {
                while (MoreTokens())
                {
                    if (CurrentToken is { Kind: TokenKind.NewLine } or { Kind: TokenKind.Whitespace })
                    {
                        break;
                    }

                    if (parameterNode is null)
                    {
                        parameterNode = new DirectiveParameterNode(_sourceText, _syntaxTree);
                        parameterNameNode = new DirectiveParameterNameNode(_sourceText, _syntaxTree);
                    }

                    ConsumeCurrentTokenInto(parameterNameNode!);
                }

                if (parameterNode is not null &&
                    parameterNameNode is not null)
                {
                    ParseTrailingWhitespace(parameterNameNode, stopBeforeNewLine: true);

                    parameterNode.Add(parameterNameNode);

                    if (ParseParameterValue() is { } argNode)
                    {
                        parameterNode.Add(argNode);
                    }
                }
            }

            if (parameterNode is not null)
            {
                return ParseTrailingWhitespace(parameterNode, stopBeforeNewLine: true);
            }
            else
            {
                return null;
            }
        }
    }

    private bool IsAtStartOfParameterName() =>
        CurrentTokenPlus(-1) is { Kind: TokenKind.Whitespace } &&
        CurrentToken is { Kind: TokenKind.Punctuation } and { Text: "-" };

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

    internal static class ErrorCodes
    {
        public const string UnknownDirective = "DNI101";
        public const string UnknownParameterName = "DNI103";
        public const string MissingRequiredParameter = "DNI104";
        public const string TooManyOccurrencesOfParameter = "DNI105";
        public const string InvalidJsonInParameterValue = "DNI106";
        public const string ParametersMustAppearAfterSubcommands = "DNI107";

        public const string MissingBindingDelegate = "DNI204";
        public const string MissingSerializationType = "DNI205";
    }
}