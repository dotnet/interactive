// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRequestParseResult
{
    public HttpRequestParseResult(HttpSyntaxTree? syntaxTree)
        => SyntaxTree = syntaxTree;

    public HttpSyntaxTree? SyntaxTree { get; }

    public IEnumerable<Diagnostic> GetDiagnostics()
    {
        return SyntaxTree?.RootNode?.GetDiagnostics() ?? Enumerable.Empty<Diagnostic>();
    }
}