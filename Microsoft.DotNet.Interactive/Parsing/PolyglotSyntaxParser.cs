// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class PolyglotSyntaxParser
    {
        public string DefaultLanguage { get; }
        private readonly SourceText _sourceText;
        private readonly Parser _directiveParser;
        private readonly IReadOnlyList<ICommand> _directives;
        private IReadOnlyList<SyntaxToken>? _tokens;
        private readonly SyntaxNode _rootNode;
        private HashSet<string>? _kernelChooserDirectives;

        internal PolyglotSyntaxParser(
            SourceText sourceText,
            string defaultLanguage,
            Parser directiveParser,
            IReadOnlyList<ICommand> directives)
        {
            DefaultLanguage = defaultLanguage;
            _sourceText = sourceText;
            _directiveParser = directiveParser;
            _directives = directives;
            _rootNode = new PolyglotSubmissionNode(defaultLanguage, _sourceText);
        }

        public PolyglotSyntaxTree Parse()
        {
            _tokens = new Lexer(_sourceText).Lex();

            ParseSubmission();

            return new PolyglotSyntaxTree(_sourceText, _rootNode);
        }

        private void ParseSubmission()
        {
            var currentLanguage = DefaultLanguage;

            DirectiveNode? directiveNode = null;

            for (var i = 0; i < _tokens!.Count; i++)
            {
                var currentToken = _tokens[i];

                switch (currentToken)
                {
                    case DirectiveToken directiveToken:

                        if (IsLanguageDirective(directiveToken))
                        {
                            directiveNode = new KernelDirectiveNode(directiveToken, _sourceText);
                            currentLanguage = directiveToken.DirectiveName;
                            _rootNode.Add(directiveNode);
                        }
                        else
                        {
                            directiveNode = new DirectiveNode(directiveToken, _sourceText);

                            if (_tokens.Count >= i + 2 &&
                                _tokens[i + 1] is TriviaToken trivia &&
                                _tokens[i + 2] is DirectiveArgsToken directiveArgs)
                            {
                                var fullDirectiveText = directiveToken.Text + trivia.Text + directiveArgs.Text;

                                i += 2;
                                var directiveParseResult = _directiveParser.Parse(fullDirectiveText);

                                directiveNode.Add(directiveArgs);
                                


                            }

                            _rootNode.Add(directiveNode);
                        }

                        break;


                    case LanguageToken languageToken:
                        var languageNode = new LanguageNode(currentLanguage, _sourceText);
                        languageNode.Add(languageToken);

                        _rootNode.Add(languageNode);

                        break;

                    case TriviaToken trivia:
                        (directiveNode ?? _rootNode).Add(trivia);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(currentToken));
                }
            }
        }

        private bool IsLanguageDirective(DirectiveToken directiveToken)
        {
            if (_kernelChooserDirectives is null)
            {
                _kernelChooserDirectives = new HashSet<string>(
                    _directives
                        .OfType<ChooseKernelDirective>()
                        .SelectMany(c => c.Aliases));
            }

            return _kernelChooserDirectives.Contains(directiveToken.Text);
        }
    }
}