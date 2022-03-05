// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Microsoft.DotNet.Interactive.CSharpProject.Models.Execution;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models
{
    public static class WorkspaceRequestFactory
    { 

        public static WorkspaceRequest CreateRequestFromDirectory(DirectoryInfo directory, string workspaceType)
        {
            var workspace = WorkspaceFactory.CreateWorkspaceFromDirectory(directory, workspaceType);

            return new WorkspaceRequest(workspace);
        }
    }
}
