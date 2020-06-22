// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class ChangeWorkingDirectory : KernelCommand
    {
        public DirectoryInfo WorkingDirectory { get; }

        public ChangeWorkingDirectory(DirectoryInfo workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            Directory.SetCurrentDirectory(WorkingDirectory.FullName);
            context.Publish(new WorkingDirectoryChanged(WorkingDirectory, this));
            return Task.CompletedTask;
        }
    }
}