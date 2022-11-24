// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

public abstract class CodeFenceOptionsParseResult
{
    internal CodeFenceOptionsParseResult()
    {
    }

    public static CodeFenceOptionsParseResult Failed(IList<string> errorMessages) => new FailedCodeFenceOptionParseResult(errorMessages);

    public static CodeFenceOptionsParseResult Failed(string errorMessage) => new FailedCodeFenceOptionParseResult(new[] { errorMessage });

    public static CodeFenceOptionsParseResult None { get; } = new NoCodeFenceOptions();

    public static CodeFenceOptionsParseResult Succeeded(CodeFenceAnnotations annotations) => new SuccessfulCodeFenceOptionParseResult(annotations);
}