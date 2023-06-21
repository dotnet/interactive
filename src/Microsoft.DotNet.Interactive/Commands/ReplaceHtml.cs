// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands;

public class ReplaceHtml : KernelCommand
{
    public ReplaceHtml(
        string elementSelector,
        string replacementHtml,
        string targetKernelName = null) : base(targetKernelName)
    {
        ElementSelector = elementSelector ?? throw new ArgumentNullException(nameof(elementSelector));
        ReplacementHtml = replacementHtml ?? throw new ArgumentNullException(nameof(replacementHtml));
    }

    public string ElementSelector { get; }

    public string ReplacementHtml { get; }
}