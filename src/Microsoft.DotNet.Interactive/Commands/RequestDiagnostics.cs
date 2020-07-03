// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestDiagnostics : SplittableCommand
    {
        public RequestDiagnostics(
            string code,
            string targetKernelName = null) : base(code, targetKernelName)
        {
        }

        internal RequestDiagnostics(
            LanguageNode languageNode,
            KernelCommand parent = null)
            : base(languageNode, parent)
        {
        }

        public override string ToString() => $"{nameof(RequestDiagnostics)}: {Code.TruncateForDisplay()}";
    }
}
