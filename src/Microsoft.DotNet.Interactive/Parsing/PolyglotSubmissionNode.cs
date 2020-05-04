// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public class PolyglotSubmissionNode : SyntaxNode
    {
        internal PolyglotSubmissionNode(
            string defaultLanguage,
            SourceText sourceText,
            PolyglotSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
        {
            DefaultLanguage = defaultLanguage;
        }

        public string DefaultLanguage { get; }
    }
}