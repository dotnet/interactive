// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public abstract class CodeBlockContentFetchResult
    {
        internal CodeBlockContentFetchResult()
        {
        }

        public static CodeBlockContentFetchResult Failed(IList<string> errorMessages) => new FailedCodeBlockContentFetchResult(errorMessages);

        public static CodeBlockContentFetchResult None { get; } = new ExternalContentNotEnabledResult();

        public static CodeBlockContentFetchResult Succeeded(string content) => new SuccessfulCodeBlockContentFetchResult(content);
    }
}