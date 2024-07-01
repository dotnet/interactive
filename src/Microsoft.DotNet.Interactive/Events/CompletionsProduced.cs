// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class CompletionsProduced : KernelEvent
{
    private readonly LinePositionSpan _linePositionSpan;

    public CompletionsProduced(
        IReadOnlyCollection<CompletionItem> completions,
        RequestCompletions command,
        LinePositionSpan linePositionSpan = null) : base(command)
    {
        Completions = completions ?? throw new ArgumentNullException(nameof(completions));
        _linePositionSpan = linePositionSpan;
    }

    /// <summary>
    /// The range of where to replace in a completion request.
    /// </summary>
    public LinePositionSpan LinePositionSpan => this.CalculateLineOffsetFromParentCommand(_linePositionSpan);

    /// <summary>
    /// The list of completions.
    /// </summary>
    public IEnumerable<CompletionItem> Completions { get; }
}