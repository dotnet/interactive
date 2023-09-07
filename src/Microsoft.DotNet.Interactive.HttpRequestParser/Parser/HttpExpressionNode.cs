// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpExpressionNode : HttpSyntaxNode
{
    internal HttpExpressionNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpBindingResult<object?> CreateBindingFailure(string message) =>
        HttpBindingResult<object?>.Failure(CreateDiagnostic(message));

    public HttpBindingResult<object?> CreateBindingSuccess(object? value) =>
        HttpBindingResult<object?>.Success(value);
}