// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CompletionRequestCompleted : KernelEvent
    {
        private readonly LinePositionSpan? _linePositionSpan;

        public CompletionRequestCompleted(
            IEnumerable<CompletionItem> completionList,
            RequestCompletion command,
            LinePositionSpan? linePositionSpan = null) : base(command)
        {
            CompletionList = completionList ?? throw new ArgumentNullException(nameof(completionList));
            _linePositionSpan = linePositionSpan;
        }

        /// <summary>
        /// The range of where to replace in a completion request.
        /// </summary>
        public LinePositionSpan? LinePositionSpan => this.CalculateLineOffsetFromParentCommand(_linePositionSpan);

        /// <summary>
        /// The list of completion options.
        /// </summary>
        public IEnumerable<CompletionItem> CompletionList { get; }
    }
}
