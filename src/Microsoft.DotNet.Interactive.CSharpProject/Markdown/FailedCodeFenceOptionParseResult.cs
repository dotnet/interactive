// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public sealed class FailedCodeFenceOptionParseResult : CodeFenceOptionsParseResult
    {
        public FailedCodeFenceOptionParseResult(IList<string> errorMessages)
        {
            ErrorMessages = errorMessages;
        }

        public IList<string> ErrorMessages { get; }
    }
}