// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class PolyglotSyntaxParser
    {
        public string DefaultLanguage { get; }
        private readonly SourceText _sourceText;
        private readonly Parser _rootKernelDirectiveParser;
        private readonly IDictionary<string, Func<Parser>> _subkernelDirectiveParsersByLanguageName;
        private IReadOnlyList<SyntaxToken>? _tokens;
        private HashSet<string>? _kernelChooserDirectives;

        internal PolyglotSyntaxParser(
            SourceText sourceText,
            string defaultLanguage,
            Parser rootKernelDirectiveParser,
            IDictionary<string, Func<Parser>> subkernelDirectiveParsersByLanguageName)
        {
            DefaultLanguage = defaultLanguage;
            _sourceText = sourceText;
            _rootKernelDirectiveParser = rootKernelDirectiveParser;
            _subkernelDirectiveParsersByLanguageName = subkernelDirectiveParsersByLanguageName;
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
                            directiveNode = new KernelNameDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);
                            currentLanguage = directiveToken.DirectiveName;
                        }
                        else
                        {
                            directiveNode = new ActionDirectiveNode(
                                directiveToken,
                                _sourceText,
                                currentLanguage,
                                rootNode.SyntaxTree);
                        }

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

                        var directiveName = directiveNode.First().Text;

                        if (IsDefinedInRootKernel(directiveName))
                        {
                            directiveNode.DirectiveParser = _rootKernelDirectiveParser;
                        }
                        else if (_subkernelDirectiveParsersByLanguageName != null &&
                                 _subkernelDirectiveParsersByLanguageName.TryGetValue(currentLanguage, out var getParser))
                        {
                            directiveNode.DirectiveParser = getParser();
                        }
                        else
                        {
                            directiveNode.DirectiveParser = _rootKernelDirectiveParser;
                        }

                        if (directiveToken.Text == "#r")
                        {
                            var parseResult = directiveNode.GetDirectiveParseResult();

                            if (parseResult.Errors.Count == 0)
                            {
                                var value = parseResult.CommandResult.GetArgumentValueOrDefault<PackageReferenceOrFileInfo>("package");

                                if (value?.Value is FileInfo)
                                {
                                    // #r <file> is treated as a LanguageNode to be handled by the compiler
                                    AddAsLanguageNode(directiveNode);

                                    break;
                                }
                            }
                        }

                        rootNode.Add(directiveNode);

                        break;

                    case LanguageToken languageToken:
                    {
                        var languageNode = new LanguageNode(
                            currentLanguage,
                            _sourceText,
                            rootNode.SyntaxTree);
                        languageNode.Add(languageToken);

                        rootNode.Add(languageNode);
                    }
                        break;

                    case TriviaToken trivia:
                        rootNode.Add(trivia);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(currentToken));
                }
            }

            void AddAsLanguageNode(DirectiveNode directiveNode)
            {
                var languageNode = new LanguageNode(
                    currentLanguage,
                    _sourceText,
                    rootNode.SyntaxTree);

                languageNode.Add(directiveNode);

                rootNode.Add(languageNode);
            }
        }

        private bool IsDefinedInRootKernel(string directiveName)
        {
            return _rootKernelDirectiveParser
                   .Configuration
                   .RootCommand
                   .Children
                   .Any(c => c.HasAlias(directiveName));
        }

        private bool IsLanguageDirective(DirectiveToken directiveToken)
        {
            if (_kernelChooserDirectives is null &&
                _subkernelDirectiveParsersByLanguageName != null)
            {
                _kernelChooserDirectives = new HashSet<string>(
                    _rootKernelDirectiveParser
                        .Configuration
                        .RootCommand
                        .Children
                        .OfType<ChooseKernelDirective>()
                        .SelectMany(c => c.Aliases)
                );
            }

            return _kernelChooserDirectives?.Contains(directiveToken.Text) == true;
        }
    }
}