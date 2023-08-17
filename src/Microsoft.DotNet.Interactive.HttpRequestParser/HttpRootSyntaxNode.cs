// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRootSyntaxNode : HttpSyntaxNode
{
    internal HttpRootSyntaxNode(SourceText sourceText, HttpSyntaxTree? tree) : base(sourceText, tree)
    {
    }

    public void Add(HttpRequestNode requestNode)
    {
        AddInternal(requestNode);
    }

    public void Add(HttpRequestSeparatorNode separatorNode)
    {
        AddInternal(separatorNode);
    }
}