// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CompleteCodeSubmissionReceived : KernelEventBase
    {
        public CompleteCodeSubmissionReceived(SubmitCode submitCode) : base(submitCode)
        {
        }

        public string Code => ((SubmitCode)Command).Code;

        public override string ToString() => $"{base.ToString()}: {Code.TruncateForDisplay()}";
    }
}