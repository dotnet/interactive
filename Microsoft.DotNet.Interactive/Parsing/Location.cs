// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class Location
    {
        internal Location(PolyglotSyntaxTree? sourceTree, TextSpan sourceSpan)
        {
            SourceTree = sourceTree;
            SourceSpan = sourceSpan;
        }

        public PolyglotSyntaxTree? SourceTree { get; }

        public TextSpan SourceSpan { get; }

        public FileLinePositionSpan GetLineSpan() => default;
    }
}