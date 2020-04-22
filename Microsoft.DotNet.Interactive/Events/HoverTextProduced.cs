// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public abstract class HoverTextProduced : KernelEventBase
    {
        public string Content { get; set; }
        public LinePositionSpan? Range { get; set; }

        public HoverTextProduced(IKernelCommand command, string content, LinePositionSpan? range = null)
            : base(command)
        {
            Content = content;
            Range = range;
        }
    }
}
