// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public class LanguageNode : SyntaxNode
    {
        internal LanguageNode(string language, SourceText sourceText) : base(sourceText)
        {
            Language = language;
        }

        public string Language { get; }
    }
}