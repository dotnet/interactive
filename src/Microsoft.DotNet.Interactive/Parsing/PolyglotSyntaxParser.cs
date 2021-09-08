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
using Microsoft.DotNet.Interactive.Utility;

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
            IDictionary<string, (KernelUri kernelUri, Func<Parser> getParser)>? subkernelInfoByKernelName)
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
                                var kernelUri = KernelUri.Parse(directiveToken.DirectiveName);
                                directiveToken = new DirectiveToken(_sourceText, new TextSpan(currentToken.Span.Start, kernelUri.GetLocalKernelName().Length + 2), rootNode.SyntaxTree);

                                var remoteKernelName = kernelUri.GetRemoteKernelName();
                                directiveNode =
                                    new ProxyKernelNameDirectiveNode(remoteKernelName, directiveToken, _sourceText, rootNode.SyntaxTree);
                                currentKernelIsProxy = true;
                                
                                AssignDirectiveParser(directiveNode);
                                rootNode.Add(directiveNode);
                                currentKernelName = directiveNode.KernelName;
                            }
                            else
                            {
                                directiveNode =
                                    new KernelNameDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);
                                currentKernelIsProxy = false;
                                currentKernelName = directiveToken.DirectiveName;
                            }

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
                                if (_tokens.Count > i + 1 && _tokens[i + 1] is TriviaToken triviaNode)
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
                                        var packageArg = (Argument<PackageReferenceOrFileInfo>)parseResult.CommandResult.Command.Arguments.Single(a => a.Name == "package");

                                        var value = parseResult.GetValueForArgument(packageArg);

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
                var previousSyntaxNode = rootNode.ChildNodes.LastOrDefault();
                var previousLanguageNode = previousSyntaxNode as LanguageNode;
                if (previousLanguageNode is { } &&
                    previousLanguageNode is not KernelNameDirectiveNode &&
                    previousLanguageNode is not ProxyKernelNameDirectiveNode &&
                    previousLanguageNode.KernelName == currentKernelName)
                {
                    previousLanguageNode.Add(nodeOrToken);
                    rootNode.GrowSpan(previousLanguageNode);
                }
                else
                {
                    var targetKernelName = previousLanguageNode is ProxyKernelNameDirectiveNode proxyNode
                        ? $"{proxyNode.KernelName}/{proxyNode.RemoteKernelName}".TrimEnd('/')
                        : currentKernelName ?? DefaultLanguage;
                    var languageNode = new LanguageNode(
                        targetKernelName,
                        _sourceText,
                        rootNode.SyntaxTree);
                    languageNode.KernelUri = currentKernelInfo.kernelUri;
                    languageNode.Add(nodeOrToken);

                    rootNode.Add(languageNode);
                }
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
                        .SelectMany(c => c.Aliases.Select(kernelAlias => new ChooseKernelDirectiveInfo(kernelAlias, c.Kernel is ProxyKernel)))
                        .ToDictionary(info => info.KernelAlias);
            }

            var localKernelName = kernelAlias.GetLocalKernelName();
            return _kernelChooserDirectives.TryGetValue(localKernelName, out chooseKernelDirectiveInfo);
        }

        private record ChooseKernelDirectiveInfo(string KernelAlias, bool IsProxyKernel);
    }
}