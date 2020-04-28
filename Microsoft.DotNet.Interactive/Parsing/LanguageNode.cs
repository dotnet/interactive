// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public class LanguageNode : SyntaxNode
    {
        internal LanguageNode(
            string language, 
            SourceText sourceText,
            PolyglotSyntaxTree? syntaxTree) : base(sourceText,syntaxTree)
        {
            Language = language;
        }

        public string Language { get; }

        internal LanguageSpecificParseResult LanguageSpecificParseResult { get; set; }

        public IEnumerable<Diagnostic> GetDiagnostics() =>
            LanguageSpecificParseResult?.GetDiagnostics() ??
            LanguageService.None.GetDiagnostics();
    }

    internal class LanguageService
    {
        public static LanguageSpecificParseResult None { get; } = new NoLanguageService();
    }

    internal class NoLanguageService : LanguageSpecificParseResult
    {
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            yield break;
        }
    }

    public class LanguageSpecificParseResult
    {
        public virtual IEnumerable<Diagnostic> GetDiagnostics()
        {
            yield break;
        }
    }

    internal class DirectiveLanguageService : LanguageSpecificParseResult
    {
        private ParseResult _parseResult;

        public DirectiveLanguageService(
            Parser DirectiveParser,
            DirectiveNode node)
        {
            this.DirectiveParser = DirectiveParser;
            Node = node;
        }

        public Parser DirectiveParser { get; }

        public DirectiveNode Node { get; }

        public override IEnumerable<Diagnostic> GetDiagnostics()
        {
            EnsureParsed();

            foreach (var error in _parseResult.Errors)
            {
                yield return new Diagnostic(
                    DiagnosticSeverity.Error,
                    new Location(Node.SyntaxTree, Node.Span));
            }
        }

        private void EnsureParsed()
        {
            if (_parseResult == null)
            {
                _parseResult = DirectiveParser.Parse(Node.Text);
            }
        }
    }
}