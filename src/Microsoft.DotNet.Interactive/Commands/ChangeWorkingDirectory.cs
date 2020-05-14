// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands
{
    public class ChangeWorkingDirectory : KernelCommandBase
    {
        public string WorkingDirectory { get; }

        public ChangeWorkingDirectory(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }
    }
}
