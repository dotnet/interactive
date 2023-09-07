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

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotSyntaxParser
{
    private readonly SourceText _sourceText;
    private readonly Parser _rootKernelDirectiveParser;
    private readonly IDictionary<string, (SchedulingScope commandScope, Func<Parser> getParser)> _subkernelInfoByKernelName;
    private IReadOnlyList<SyntaxToken>? _tokens;
    private HashSet<string>? _mapOfKernelNamesByAlias;

    internal PolyglotSyntaxParser(
        SourceText sourceText,
        string? defaultLanguage,
        Parser rootKernelDirectiveParser,
        IDictionary<string, (SchedulingScope commandScope, Func<Parser> getParser)>? subkernelInfoByKernelName = null)
    {
        DefaultLanguage = defaultLanguage ?? "";
        _sourceText = sourceText;
        _rootKernelDirectiveParser = rootKernelDirectiveParser;
        _subkernelInfoByKernelName = subkernelInfoByKernelName ?? new ConcurrentDictionary<string, (SchedulingScope commandScope, Func<Parser> getParser)>();
    }

    public string DefaultLanguage { get; }

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

        for (var i = 0; i < _tokens!.Count; i++)
        {
            var currentToken = _tokens[i];

            switch (currentToken)
            {
                case DirectiveToken directiveToken:

                    DirectiveNode? directiveNode;

                    if (IsChooseKernelDirective(directiveToken))
                    {
                        directiveNode =
                            new KernelNameDirectiveNode(directiveToken, _sourceText, rootNode.SyntaxTree);

                        currentKernelName = directiveToken.DirectiveName;

                        if (_subkernelInfoByKernelName.TryGetValue(currentKernelName, out currentKernelInfo))
                        {
                            directiveNode.CommandScope = currentKernelInfo.commandScope;
                        }
                    }
                    else
                    {
                        directiveNode = new ActionDirectiveNode(
                            directiveToken,
                            _sourceText,
                            currentKernelName ?? DefaultLanguage,
                            rootNode.SyntaxTree);

                        directiveNode.AllowValueSharingByInterpolation = AllowsValueSharingByInterpolation(directiveToken);
                    }

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
                            directiveNode.CommandScope = currentKernelInfo.commandScope;
                        }

                        if (parseResult.Errors.Count == 0)
                        {
                            var value = parseResult.GetValueForArgument(parseResult.Parser.FindPackageArgument());

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
            var previousSyntaxNode = rootNode.ChildNodes.LastOrDefault();
            var previousLanguageNode = previousSyntaxNode as LanguageNode;
            if (previousLanguageNode is { } &&
                previousLanguageNode is not KernelNameDirectiveNode &&
                previousLanguageNode.Name == currentKernelName)
            {
                previousLanguageNode.Add(nodeOrToken);
                rootNode.GrowSpan(previousLanguageNode);
            }
            else
            {
                var targetKernelName = currentKernelName ?? DefaultLanguage;
                var languageNode = new LanguageNode(
                    targetKernelName,
                    _sourceText,
                    rootNode.SyntaxTree);
                languageNode.CommandScope = currentKernelInfo.commandScope;
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
            .OfType<IdentifierSymbol>()
            .Any(c => c.HasAlias(directiveName));
    }

    private bool IsChooseKernelDirective(DirectiveToken directiveToken)
    {
        if (_mapOfKernelNamesByAlias is null)
        {
            _mapOfKernelNamesByAlias =
                new HashSet<string>(_rootKernelDirectiveParser
                    .Configuration
                    .RootCommand
                    .Children
                    .OfType<ChooseKernelDirective>()
                    .SelectMany(c => c.Aliases));
        }

        return _mapOfKernelNamesByAlias.Contains(directiveToken.Text);
    }

    private bool AllowsValueSharingByInterpolation(DirectiveToken directiveToken) => 
        directiveToken.DirectiveName != "set";
}