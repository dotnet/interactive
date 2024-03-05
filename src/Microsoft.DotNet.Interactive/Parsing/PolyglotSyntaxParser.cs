// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
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
    private readonly Parser _rootKernelDirectiveParser;
    private readonly IDictionary<string, (string scope, Func<Parser> getParser)> _subkernelInfoByKernelName;
    private IReadOnlyList<SyntaxToken>? _tokens;
    private HashSet<string>? _mapOfKernelNamesByAlias;
    private readonly Func<IReadOnlyCollection<string>?> _getMapOfKernelNamesByAlias;

    public static PolyglotSyntaxTree Parse(string code, string defaultLanguage )
    {
        var parser = new PolyglotSyntaxParser(SourceText.From(code), defaultLanguage, null, null);

        var tree = parser.Parse();

        return tree;
    }

    internal PolyglotSyntaxParser(
        SourceText sourceText,
        string? defaultLanguage,
        Parser rootKernelDirectiveParser,
        Func<IReadOnlyCollection<string>?>? getMapOfKernelNamesByAlias = null,
        IDictionary<string, (string scope, Func<Parser> getParser)>? subkernelInfoByKernelName = null)
    {
        _sourceText = sourceText;
        _rootKernelDirectiveParser = rootKernelDirectiveParser;
        _getMapOfKernelNamesByAlias = getMapOfKernelNamesByAlias ?? (() => Array.Empty<string>());
        _subkernelInfoByKernelName = subkernelInfoByKernelName ?? new ConcurrentDictionary<string, (string scope, Func<Parser> getParser)>();
    }

    public PolyglotSyntaxTree Parse()
    {
        var tree = new PolyglotSyntaxTree(_sourceText, DefaultLanguage);

        while (MoreTokens())
        {
            if (ParseDirective() is { } directiveNode)
            {
                _syntaxTree.RootNode.Add(directiveNode);
            }
            else if (ParseLanguageNode() is { } languageNode)
            {
                _syntaxTree.RootNode.Add(languageNode);
            }
        }

        var rootNode = new PolyglotSubmissionNode(
            _sourceText,
            tree);

        ParseSubmission(rootNode);

            var directiveNode = new DirectiveNode(_sourceText, _syntaxTree);
            directiveNode.TargetKernelName = targetKernelName;

    private void ParseSubmission(PolyglotSubmissionNode rootNode)
    {
        var currentKernelName = DefaultLanguage;
        // FIX: (ParseSubmission) rewrite

                switch (directive)
                {
                    case KernelSpecifierDirective kernelSpecifier:
                        directiveNode.TargetKernelName = kernelSpecifier.KernelName;
                        directiveNode.Kind = DirectiveNodeKind.KernelSelector;
                        _currentKernelName = kernelSpecifier.KernelName;

                        break;

                    case KernelActionDirective:
                        if (directive.ParentKernelInfo is { } parentKernelInfo)
                        {
                            directiveNode.TargetKernelName = parentKernelInfo.LocalName;
                        }

                        directiveNode.Kind = DirectiveNodeKind.Action;

                        break;
                }
            }

            directiveNode.Add(directiveNameNode);

            while (MoreTokens())
            {
                if (CurrentToken is null)
                {
                    break;
                }

                if (CurrentToken is { Kind: TokenKind.NewLine })
                {
                    ConsumeCurrentTokenInto(directiveNode);
                    break;
                }

                if (IsAtStartOfParameterName())
                {
                    if (ParseNamedParameter() is { } namedParameterNode)
                    {
                        directiveNode.Add(namedParameterNode);
                    }
                }
                else if (CurrentToken is { Kind: TokenKind.Word } word &&
                         _currentlyScopedDirective is KernelActionDirective actionDirective &&
                         actionDirective.TryGetSubcommand(word.Text, out var subcommand))
                {
                    _currentlyScopedDirective = subcommand;
                    var subcommandNode = new DirectiveSubcommandNode(_sourceText, _syntaxTree);
                    ConsumeCurrentTokenInto(subcommandNode);
                    ParseTrailingWhitespace(subcommandNode);
                    directiveNode.Add(subcommandNode);
                }
                else if (ParseParameterValue() is { } parameterValueNode)
                {
                    if (_currentlyScopedParameter?.Flag is true)
                    {
                        directiveNode.Add(parameterValueNode);
                    }
                    else
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

            return directiveNode;
        }

        return null;

        DirectiveNameNode ParseParameterName()
        {
            var directiveNameNode = new DirectiveNameNode(_sourceText, _syntaxTree);

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

        void AssignDirectiveParser(DirectiveNode directiveNode)
        {
            var directiveName = directiveNode.ChildNodesAndTokens[0].Text;

            if (IsDefinedInRootKernel(directiveName))
            {
                directiveNode.DirectiveParser = _rootKernelDirectiveParser;
            }
            else if (_subkernelInfoByKernelName.TryGetValue(currentKernelName ?? string.Empty,
                                                            out var info))
            {
                directiveNode.DirectiveParser = info.getParser();
            }
            else
            {
                directiveNode.DirectiveParser = _rootKernelDirectiveParser;
            }
        }
    }

    private bool IsDefinedInRootKernel(string directiveName)
    {
        return _rootKernelDirectiveParser
               .Configuration
               .RootCommand
               .Children
               .OfType<IdentifierSymbol>()
               .Any(c => c.HasAlias(directiveName));
    }

    private bool IsChooseKernelDirective(DirectiveNode directiveNode)
    {
        if (_mapOfKernelNamesByAlias is null)
        {
            _mapOfKernelNamesByAlias = new(_getMapOfKernelNamesByAlias());
        }

        return _mapOfKernelNamesByAlias.Contains(directiveNode.Text);
    }

    private bool AllowsValueSharingByInterpolation(DirectiveNode directiveNode) =>
        directiveNode.Text != "set";

    internal class PolyglotLexer
    {
        private TextWindow _textWindow;
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

            while (More())
            {
                LexTrivia();

                LexSyntax();
            }

            return _tokens;
        }

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

        private void LexDirective()
        {
            // if (!_textWindow.IsEmpty)
            // {
            //     FlushToken(TokenKind.Language);
            // }
            //
            // while (More())
            // {
            //     switch (GetNextChar())
            //     {
            //         case ' ':
            //         case '\t':
            //             FlushToken(TokenKind.Directive);
            //             LexDirectiveArgs();
            //             return;
            //
            //         case '\r':
            //         case '\n':
            //             FlushToken(TokenKind.Directive);
            //             LexTrivia();
            //             return;
            //
            //         default:
            //             _textWindow.Advance();
            //             break;
            //     }
            // }
            //
            // FlushToken(TokenKind.Directive);
        }

        private void LexDirectiveArgs()
        {
            // var inTrivia = true;
            // var foundArgs = false;
            //
            // while (More())
            // {
            //     var next = GetNextChar();
            //
            //     switch (next)
            //     {
            //         case ' ' when inTrivia:
            //         case '\t'  when inTrivia:
            //             _textWindow.Advance();
            //             break;
            //
            //         case '\r':
            //         case '\n':
            //             if (foundArgs)
            //             {
            //                 FlushToken(TokenKind.DirectiveArgs);
            //                 LexTrivia();
            //             }
            //             else
            //             {
            //                 FlushToken(TokenKind.Trivia);
            //             }
            //
            //             return;
            //
            //         default:
            //             if (inTrivia)
            //             {
            //                 FlushToken(TokenKind.Trivia);
            //                 inTrivia = false;
            //             }
            //             else
            //             {
            //                 _textWindow.Advance();
            //             }
            //
            //             foundArgs = true;
            //
            //             break;
            //     }
            // }
            //
            // FlushToken(TokenKind.DirectiveArgs);
        }

        private void LexSyntax()
        {
            // while (More())
            // {
            //     if (IsDirective())
            //     {
            //         LexDirective();
            //     }
            //     else if(More())
            //     {
            //         _textWindow.Advance();
            //     }
            // }
            //
            // FlushToken(TokenKind.Language);
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

    internal static class ErrorCodes
    {
        // parsing errors
        public const string UnknownDirective = "DNI101";
        public const string UnknownParameterName = "DNI103";
        public const string MissingRequiredParameter = "DNI104";
        public const string TooManyOccurrencesOfParameter = "DNI105";
        public const string InvalidJsonInParameterValue = "DNI106";
        public const string ParametersMustAppearAfterSubcommands = "DNI107";

        // magic command usage errors
        public const string UnsupportedMimeType = "DNI201";
        public const string ValueNotFoundInKernel = "DNI202";
        public const string ByRefNotSupportedWithProxyKernels = "DNI203";
        public const string InputNotProvided = "DNI204";
        public const string FromUrlAndFromFileCannotBeUsedTogether = "DNI205";
        public const string FromUrlAndFromValueCannotBeUsedTogether = "DNI206";
        public const string FromFileAndFromValueCannotBeUsedTogether = "DNI207";

        // API usage errors
        public const string MissingBindingDelegate = "DNI301";
        public const string MissingSerializationType = "DNI302";
        public const string ByRefAndMimeTypeCannotBeCombined = "DNI303";
    }
}