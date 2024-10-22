// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Directives;

public class KernelDirectiveCompletionContext
{
    public IList<CompletionItem> CompletionItems { get; } = new List<CompletionItem>();
}