// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.LanguageService;

namespace Microsoft.DotNet.Interactive.Events
{
    public class HoverTextProduced : KernelEventBase
    {
        public MarkupContent Contents { get; set; }
        public Range Range { get; set; }

        public HoverTextProduced(IKernelCommand command, MarkupContent contents, Range range =  null)
            : base(command)
        {
            Contents = contents;
            Range = range;
        }
    }
}
