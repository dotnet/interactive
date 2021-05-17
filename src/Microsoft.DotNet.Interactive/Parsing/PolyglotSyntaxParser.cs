// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Connection;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class PolyglotSyntaxParser
    {
        public string DefaultLanguage { get; }
        private readonly SourceText _sourceText;
        private readonly Parser _rootKernelDirectiveParser;
        private readonly IDictionary<string, (KernelUri kernelUri, Func<Parser> getParser)> _subkernelInfoByKernelName;
        private IReadOnlyList<SyntaxToken>? _tokens;
        private Dictionary<string, ChooseKernelDirectiveInfo>? _kernelChooserDirectives;

        internal PolyglotSyntaxParser(
            SourceText sourceText,
            string defaultLanguage,
            Parser rootKernelDirectiveParser,
            IDictionary<string, (KernelUri kernelUri, Func<Parser> getParser)> subkernelInfoByKernelName)
        {
            DefaultLanguage = defaultLanguage;
            _sourceText = sourceText;
            _rootKernelDirectiveParser = rootKernelDirectiveParser;
            _subkernelInfoByKernelName = subkernelInfoByKernelName ?? new ConcurrentDictionary<string, (KernelUri kernelUri, Func<Parser> getParser)>();
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
            _subkernelInfoByKernelName.TryGetValue(currentKernelName ?? "", out var currentKernelInfo);
            
            var currentKernelIsProxy = false;
            if (TryGetChooseKernelDirectiveInfo(currentKernelName?? "", out var chooseKernelDirectiveInfo))
            {
                currentKernelIsProxy = chooseKernelDirectiveInfo.IsProxyKernel;
            }

            for (var i = 0; i < _tokens!.Count; i++)
            {
                var currentToken = _tokens[i];

                switch (currentToken)
                {
                    case DirectiveToken directiveToken:

                        DirectiveNode? directiveNode;

                        if (IsChooseKernelDirective(directiveToken, out var isProxyKernel))
                        {
                            if (isProxyKernel)
                            {
                                directiveNode =
                                    new ProxyKernelNameDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);
                                currentKernelIsProxy = true;
                                
                                AssignDirectiveParser(directiveNode);
                                rootNode.Add(directiveNode);
                            }
                            else
                            {
                                directiveNode =
                                    new KernelNameDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);
                                currentKernelIsProxy = false;
                            }

                            currentKernelName = directiveToken.DirectiveName;
                            if (_subkernelInfoByKernelName.TryGetValue(currentKernelName ?? string.Empty, out currentKernelInfo))
                            {
                                directiveNode.KernelUri = currentKernelInfo.kernelUri;
                            }
                        }
                        else
                        {
                            directiveNode = new ActionDirectiveNode(
                                directiveToken,
                                _sourceText,
                                currentKernelName ?? DefaultLanguage,
                                rootNode.SyntaxTree);
                            if (_subkernelInfoByKernelName.TryGetValue(directiveNode.KernelName ?? string.Empty,
                                out currentKernelInfo))
                            {
                                directiveNode.KernelUri = currentKernelInfo.kernelUri;
                            }

                        }

                        switch (directiveNode)
                        {
                            case { } _ when !currentKernelIsProxy:
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

                                AssignDirectiveParser(directiveNode);

                                if (directiveToken.Text == "#r")
                                {
                                    var parseResult = directiveNode.GetDirectiveParseResult();
                                    if (_subkernelInfoByKernelName.TryGetValue(currentKernelName ?? string.Empty,
                                        out currentKernelInfo))
                                    {
                                        directiveNode.KernelUri = currentKernelInfo.kernelUri;
                                    }

                                    if (parseResult.Errors.Count == 0)
                                    {
                                        var value = parseResult.ValueForArgument<PackageReferenceOrFileInfo>("package");

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
                            case { } node when currentKernelIsProxy && node is not ProxyKernelNameDirectiveNode:
                                AppendAsLanguageNode(node);
                                break;
                        }

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
                    previousLanguageNode is not KernelNameDirectiveNode &&
                    previousLanguageNode is not ProxyKernelNameDirectiveNode &&
                    previousLanguageNode.KernelName == currentKernelName)
                {
                    previousLanguageNode.Add(nodeOrToken);
                    rootNode.GrowSpan(previousLanguageNode);
                }
                else
                {
                    var languageNode = new LanguageNode(
                        currentKernelName ?? DefaultLanguage,
                        _sourceText,
                        rootNode.SyntaxTree);
                    languageNode.KernelUri = currentKernelInfo.kernelUri;
                    languageNode.Add(nodeOrToken);

                    rootNode.Add(languageNode);
                }
            }

            void AssignDirectiveParser(DirectiveNode? directiveNode)
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
                   .OfType<IIdentifierSymbol>()
                   .Any(c => c.HasAlias(directiveName));
        }

        private bool IsChooseKernelDirective(DirectiveToken directiveToken, out bool isProxyKernel)
        {
            if (TryGetChooseKernelDirectiveInfo(directiveToken.Text, out var chooseKernelDirectiveInfo))
            {
                isProxyKernel = chooseKernelDirectiveInfo.IsProxyKernel;
                return true;
            }

            isProxyKernel = false;
            return false;
        }

        private bool TryGetChooseKernelDirectiveInfo(string kernelAlias, out ChooseKernelDirectiveInfo chooseKernelDirectiveInfo)
        {
            if (_kernelChooserDirectives is null)
            {
                _kernelChooserDirectives =
                    _rootKernelDirectiveParser
                        .Configuration
                        .RootCommand
                        .Children
                        .OfType<ChooseKernelDirective>()
                        .SelectMany(c => c.Aliases.Select(kernelAlias => new ChooseKernelDirectiveInfo(kernelAlias, c.Kernel is ProxyKernel2)))
                        .ToDictionary(info => info.KernelAlias);
            }


            return _kernelChooserDirectives.TryGetValue(kernelAlias, out chooseKernelDirectiveInfo);
        }

        private record ChooseKernelDirectiveInfo(string KernelAlias, bool IsProxyKernel);
    }
}