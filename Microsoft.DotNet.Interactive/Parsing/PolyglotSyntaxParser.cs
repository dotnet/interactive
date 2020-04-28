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
        }

        public PolyglotSyntaxTree Parse()
        {
            var tree = new PolyglotSyntaxTree(_sourceText);

            _tokens = new PolyglotLexer(_sourceText, tree).Lex();

            var rootNode = new PolyglotSubmissionNode(
                DefaultLanguage,
                _sourceText,
                tree);

            tree.RootNode = rootNode;

            ParseSubmission(rootNode);

            return tree;
        }

        private void ParseSubmission(PolyglotSubmissionNode rootNode)
        {
            var currentLanguage = DefaultLanguage;

            for (var i = 0; i < _tokens!.Count; i++)
            {
                var currentToken = _tokens[i];

                switch (currentToken)
                {
                    case DirectiveToken directiveToken:

                        DirectiveNode directiveNode;

                        if (IsLanguageDirective(directiveToken))
                        {
                            directiveNode = new KernelDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);
                            currentLanguage = directiveToken.DirectiveName;
                        }
                        else
                        {
                            directiveNode = new DirectiveNode(
                                directiveToken,
                                _sourceText,
                                rootNode.SyntaxTree);
                        }

                        rootNode.Add(directiveNode);

                        if (_tokens.Count > i + 1 &&
                            _tokens[i + 1] is TriviaToken triviaNode)
                        {
                            i += 1;

                            directiveNode.Add(triviaNode);
                        }

                        if (_tokens.Count > i + 1 &&
                            _tokens[i + 1] is DirectiveArgsToken directiveArgs)
                        {
                            i += 1;

                            directiveNode.Add(directiveArgs);
                        }

                        directiveNode.DirectiveParser = _directiveParser;

                        break;

                    case LanguageToken languageToken:
                        var languageNode = new LanguageNode(
                            currentLanguage,
                            _sourceText,
                            rootNode.SyntaxTree);
                        languageNode.Add(languageToken);

                        rootNode.Add(languageNode);

                        break;

                    case TriviaToken trivia:
                        rootNode.Add(trivia);
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