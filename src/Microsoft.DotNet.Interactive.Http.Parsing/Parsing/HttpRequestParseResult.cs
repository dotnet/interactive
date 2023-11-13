// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpRequestParseResult
{
    public HttpRequestParseResult(HttpSyntaxTree syntaxTree)
        => SyntaxTree = syntaxTree ?? throw new ArgumentNullException(nameof(syntaxTree));

    public HttpSyntaxTree SyntaxTree { get; }

    public IEnumerable<Diagnostic> GetDiagnostics()
        => SyntaxTree.RootNode?.GetDiagnostics() ?? Enumerable.Empty<Diagnostic>();
}