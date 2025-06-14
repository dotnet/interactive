// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Events;

public class HoverTextProduced : KernelEvent
{
    private readonly LinePositionSpan _linePositionSpan;

    public HoverTextProduced(RequestHoverText command, IReadOnlyCollection<FormattedValue> content, LinePositionSpan linePositionSpan = null)
        : base(command)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (content.Count == 0)
        {
            throw new ArgumentException("At least one content required.", nameof(content));
        }

        Content = content;
        _linePositionSpan = linePositionSpan;
    }

    public IReadOnlyCollection<FormattedValue> Content { get; }

    public LinePositionSpan LinePositionSpan => this.CalculateLineOffsetFromParentCommand(_linePositionSpan);
}