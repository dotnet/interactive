// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class ChangeWorkspaceDirectory : KernelCommand
    {
        public string WorkspaceDirectory { get; }

        public ChangeWorkspaceDirectory(string workspaceDirectory)
        {
            WorkspaceDirectory = workspaceDirectory;
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            Directory.SetCurrentDirectory(WorkspaceDirectory);
            context.Publish(new WorkspaceDirectoryChanged(WorkspaceDirectory, this));            
            return Handler(this, context);
        }
    }
}