// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpHeaderNode : HttpSyntaxNode
{
    internal HttpHeaderNode(
        string sourceText,
        HttpSyntaxTree? syntaxTree,
        HttpHeaderNameNode nameNode,
        HttpHeaderSeparatorNode separatorNode,
        HttpHeaderValueNode valueNode) : base(sourceText, syntaxTree)
    {
        NameNode = nameNode;
        Add(NameNode);

        SeparatorNode = separatorNode;
        Add(SeparatorNode);

        ValueNode = valueNode;
        Add(ValueNode);
    }

    public HttpHeaderNameNode NameNode { get; }
    public HttpHeaderSeparatorNode SeparatorNode { get; }
    public HttpHeaderValueNode ValueNode { get; }
}
