// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class ChangeWorkingDirectory : KernelCommand
    {
        public string WorkingDirectory { get; }

        public ChangeWorkingDirectory(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        protected override Task OnInvokeAsync(KernelInvocationContext context)
        {
            Directory.SetCurrentDirectory(WorkingDirectory);
            context.Publish(new WorkingDirectoryChanged(WorkingDirectory, this));
            return Task.CompletedTask;
        }
    }
}