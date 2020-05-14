// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class ChangeWorkingDirectory : KernelCommandBase
    {
        public DirectoryInfo WorkingDirectory { get; }

        public ChangeWorkingDirectory(DirectoryInfo workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }
    }
}
