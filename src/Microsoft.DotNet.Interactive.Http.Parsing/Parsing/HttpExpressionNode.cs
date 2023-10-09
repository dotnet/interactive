// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpExpressionNode : HttpSyntaxNode
{
    internal HttpExpressionNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpBindingResult<object?> CreateBindingFailure(HttpDiagnosticInfo diagnosticInfo) =>
        HttpBindingResult<object?>.Failure(CreateDiagnostic(diagnosticInfo));

    public HttpBindingResult<object?> CreateBindingSuccess(object? value) =>
        HttpBindingResult<object?>.Success(value);
}