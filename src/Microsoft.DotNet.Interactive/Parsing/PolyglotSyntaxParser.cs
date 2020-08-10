﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
        private readonly IDictionary<string, Func<Parser>> _subkernelDirectiveParsersByKernelName;
        private IReadOnlyList<SyntaxToken>? _tokens;
        private HashSet<string>? _kernelChooserDirectives;

        internal PolyglotSyntaxParser(
            SourceText sourceText,
            string defaultLanguage,
            Parser rootKernelDirectiveParser,
            IDictionary<string, Func<Parser>> subkernelDirectiveParsersByKernelName)
        {
            DefaultLanguage = defaultLanguage;
            _sourceText = sourceText;
            _rootKernelDirectiveParser = rootKernelDirectiveParser;
            _subkernelDirectiveParsersByKernelName = subkernelDirectiveParsersByKernelName;
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
            var currentKernelName = DefaultLanguage;

            for (var i = 0; i < _tokens!.Count; i++)
            {
                var currentToken = _tokens[i];

                switch (currentToken)
                {
                    case DirectiveToken directiveToken:

                        DirectiveNode directiveNode;

                        if (IsChooseKernelDirective(directiveToken))
                        {
                            directiveNode = new KernelNameDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);
                            currentKernelName = directiveToken.DirectiveName;
                        }
                        else
                        {
                            directiveNode = new ActionDirectiveNode(
                                directiveToken,
                                _sourceText,
                                currentKernelName,
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

                        var directiveName = directiveNode.ChildNodesAndTokens[0].Text;

                        if (IsDefinedInRootKernel(directiveName))
                        {
                            directiveNode.DirectiveParser = _rootKernelDirectiveParser;
                        }
                        else if (_subkernelDirectiveParsersByKernelName != null &&
                                 _subkernelDirectiveParsersByKernelName.TryGetValue(currentKernelName, out var getParser))
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
                                    AppendAsLanguageNode(directiveNode);

                                    break;
                                }
                            }
                        }

                        rootNode.Add(directiveNode);

                        break;

                    case LanguageToken languageToken:
                        AppendAsLanguageNode(languageToken);
                        break;

                    case TriviaToken trivia:
                        rootNode.Add(trivia);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(currentToken));
                }
            }

            void AppendAsLanguageNode(SyntaxNodeOrToken nodeOrToken)
            {
                if (rootNode.ChildNodes.LastOrDefault() is LanguageNode previousLanguageNode &&
                    previousLanguageNode.KernelName == currentKernelName)
                {
                    previousLanguageNode.Add(nodeOrToken);
                    rootNode.GrowSpan(previousLanguageNode);
                }
                else
                {
                    var languageNode = new LanguageNode(
                        currentKernelName,
                        _sourceText,
                        rootNode.SyntaxTree);

                    languageNode.Add(nodeOrToken);

                    rootNode.Add(languageNode);
                }
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

        private bool IsChooseKernelDirective(DirectiveToken directiveToken)
        {
            if (_kernelChooserDirectives is null)
            {
                _kernelChooserDirectives = new HashSet<string>(
                    _rootKernelDirectiveParser
                        .Configuration
                        .RootCommand
                        .Children
                        .OfType<ChooseKernelDirective>()
                        .SelectMany(c => c.Aliases));
            }

            return _kernelChooserDirectives?.Contains(directiveToken.Text) == true;
        }
    }
}