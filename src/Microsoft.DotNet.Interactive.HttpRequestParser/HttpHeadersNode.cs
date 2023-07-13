// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpHeadersNode : HttpSyntaxNode
{
    internal HttpHeadersNode(string sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }
}