// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public class AddCell : KernelCommand
    {
        public string Language { get; }
        public string Contents { get; }

        public AddCell(string language, string contents, string targetKernelName = null)
            : base(targetKernelName)
        {
            Language = language;
            Contents = contents;
        }
    }
}
