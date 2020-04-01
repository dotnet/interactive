// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class PolyglotSyntaxParser
    {
        public string DefaultLanguage { get; }
        private readonly SourceText _sourceText;
        private readonly IReadOnlyList<ICommand> _directives;
        private IReadOnlyList<SyntaxToken>? _tokens;
        private readonly SyntaxNode _rootNode;
        private HashSet<string>? _kernelChooserDirectives;

        internal PolyglotSyntaxParser(
            SourceText sourceText,
            string defaultLanguage,
            IReadOnlyList<ICommand> directives)
        {
            DefaultLanguage = defaultLanguage;
            _sourceText = sourceText;
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
            DirectiveNode? directiveNode = null;

            var currentLanguage = DefaultLanguage;

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
                        }
                        else
                        {
                            directiveNode = new DirectiveNode(directiveToken, _sourceText);
                        }

                        _rootNode.Add(directiveNode);

                        break;

                    case DirectiveArgsToken directiveArgs:
                        directiveNode!.Add(directiveArgs);
                        directiveNode = null;
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