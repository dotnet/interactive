// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    public abstract class DirectiveNode : LanguageNode
    {
        private DirectiveParseResult? _parseResult;

       
        internal DirectiveNode(
            DirectiveToken directiveToken,
            SourceText sourceText,
            PolyglotSyntaxTree? syntaxTree) : base(directiveToken.DirectiveName, sourceText, syntaxTree)
        {
            Add(directiveToken);
        }

        internal DirectiveParser? DirectiveParser { get; set; }

        public DirectiveParseResult GetDirectiveParseResult()
        {
            if (DirectiveParser is null)
            {
                throw new InvalidOperationException($"{nameof(DirectiveParser)} was not set.");
            }

            return _parseResult ??= DirectiveParser.Parse(Text);
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