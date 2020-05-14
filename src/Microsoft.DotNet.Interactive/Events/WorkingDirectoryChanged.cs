// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class WorkingDirectoryChanged : KernelEventBase
    {
        public DirectoryInfo WorkingDirectory { get; }

        public WorkingDirectoryChanged(IKernelCommand command, DirectoryInfo workingDirectory)
            : base(command)
        {
            WorkingDirectory = workingDirectory;
        }
    }
}
