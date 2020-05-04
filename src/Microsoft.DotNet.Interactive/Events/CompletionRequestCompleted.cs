// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CompletionRequestCompleted : KernelEventBase
    {
        /// <summary>
        /// The start index (inclusive) of where to replace in a completion request.
        /// </summary>
        public int? ReplacementStartIndex { get; }

        /// <summary>
        /// The end index (exclusive) of where to replace in a completion request.
        /// </summary>
        public int? ReplacementEndIndex { get; }

        /// <summary>
        /// The list of completion options.
        /// </summary>
        public IEnumerable<CompletionItem> CompletionList { get; }

        public CompletionRequestCompleted(
            IEnumerable<CompletionItem> completionList,
            IKernelCommand command,
            int? replacementStartIndex = null,
            int? replacementEndIndex = null) : base(command)
        {
            CompletionList = completionList ?? throw new ArgumentNullException(nameof(completionList));

            // replacementStartIndex and replacementEndIndex must either both be null
            // or both be defined.
            if (replacementStartIndex != null && replacementEndIndex == null)
            {
                throw new ArgumentNullException(nameof(replacementEndIndex));
            }

            if (replacementStartIndex == null && replacementEndIndex != null)
            {
                throw new ArgumentNullException(nameof(replacementStartIndex));
            }

            ReplacementStartIndex = replacementStartIndex;
            ReplacementEndIndex = replacementEndIndex;
        }
    }
}
