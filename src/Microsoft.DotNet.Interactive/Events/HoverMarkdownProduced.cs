// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class HoverMarkdownProduced : HoverTextProduced
    {
        public HoverMarkdownProduced(IKernelCommand command, string content, LinePositionSpan? range = null)
            : base(command, content, range)
        {
        }
    }
}
