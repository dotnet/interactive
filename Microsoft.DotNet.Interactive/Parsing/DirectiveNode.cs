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
    public class DirectiveNode : LanguageNode
    {
        private ParseResult? _parseResult;

        internal DirectiveNode(
            DirectiveToken directiveToken,
            SourceText sourceText,
            PolyglotSyntaxTree? syntaxTree) : base("#!-directive", sourceText, syntaxTree)
        {
            Add(directiveToken);
        }

        internal Parser? DirectiveParser { get; set; }

        public ParseResult GetDirectiveParseResult()
        {
            if (_parseResult == null)
            {
                _parseResult = DirectiveParser.Parse(Text);
            }

            return _parseResult;
        }

        public override IEnumerable<Diagnostic> GetDiagnostics()
        {
            var parseResult = GetDirectiveParseResult();

            foreach (var error in parseResult.Errors)
            {
                yield return new Diagnostic(
                    error.Message,
                    DiagnosticSeverity.Error,
                    new Location(SyntaxTree, Span));
            }
        }
    }
}