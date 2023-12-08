// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Http.Parsing;

namespace Microsoft.DotNet.Interactive.Parsing;

internal abstract partial class SyntaxNodeOrToken
{
    private const string DiagnosticCategory = "HTTP";

    private protected SyntaxNodeOrToken(SourceText sourceText, SyntaxTree? syntaxTree)
    {
        SourceText = sourceText;
        SyntaxTree = (HttpSyntaxTree?)syntaxTree;
    }

    public HttpSyntaxTree? SyntaxTree { get; }
}