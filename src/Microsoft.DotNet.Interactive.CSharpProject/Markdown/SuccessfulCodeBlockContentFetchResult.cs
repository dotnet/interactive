// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

internal sealed class SuccessfulCodeBlockContentFetchResult : CodeBlockContentFetchResult
{
    public SuccessfulCodeBlockContentFetchResult(string content)
    {
        Content = content;
    }

    public string Content { get; }
}