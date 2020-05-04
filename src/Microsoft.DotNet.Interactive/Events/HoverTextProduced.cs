// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class HoverTextProduced : KernelEventBase
    {
        public IReadOnlyCollection<FormattedValue> Content { get; }
        public LinePositionSpan? Range { get; set; }

        public HoverTextProduced(IKernelCommand command, IReadOnlyCollection<FormattedValue> content, LinePositionSpan? range = null)
            : base(command)
        {
            if (content.Count == 0)
            {
                throw new ArgumentException(nameof(content), "At least one content required.");
            }

            Content = content;
            Range = range;
        }
    }
}
