// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public abstract class SyntaxToken : SyntaxNodeOrToken
    {
        internal SyntaxToken(
            SourceText sourceText,
            TextSpan span) : base(sourceText)
        {
            Span = span;
        }

        public override TextSpan Span { get; }

        private string GetDebuggerDisplay()
        {
            return GetType().Name + ": " + this;
        }

        public override string ToString() => Text;
    }
}