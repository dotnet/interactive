// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpSyntaxTree
{
    private readonly SourceText _sourceText;

    public HttpSyntaxTree(SourceText sourceText)
        => _sourceText = sourceText;

    public HttpRootSyntaxNode? RootNode { get; set; }
}
