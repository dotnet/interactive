// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpRequestParseResult
{
    public HttpRequestParseResult(HttpSyntaxTree syntaxTree)
        => SyntaxTree = syntaxTree ?? throw new ArgumentNullException(nameof(syntaxTree));

    public HttpSyntaxTree SyntaxTree { get; }

    public IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
        => SyntaxTree.RootNode?.GetDiagnostics() ?? Enumerable.Empty<CodeAnalysis.Diagnostic>();
}