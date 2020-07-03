// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class SubmitCode : SplittableCommand
    {
        public SubmitCode(
            string code,
            string targetKernelName = null,
            SubmissionType submissionType = SubmissionType.Run) : base(code, targetKernelName)
        {
        }

        internal SubmitCode(
            LanguageNode languageNode,
            SubmissionType submissionType = SubmissionType.Run,
            KernelCommand parent = null)
            : base(languageNode, parent)
        {
        }

        // FIX: (SubmitCode) remove SubmissionType
        public SubmissionType SubmissionType { get; }

        public override string ToString() => $"{nameof(SubmitCode)}: {Code.TruncateForDisplay()}";
    }
}
