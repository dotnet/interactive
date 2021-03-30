// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class WorkspaceDirectoryChanged : KernelEvent
    {
        public WorkspaceDirectoryChanged(string workspaceDirectory, KernelCommand command)
            : base(command)
        {
            WorkspaceDirectory = workspaceDirectory;
        }

        public string WorkspaceDirectory { get; }
    }
}