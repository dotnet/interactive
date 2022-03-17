// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class OpenProject : KernelCommand
    {
        public Project Project { get; }

        public OpenProject(Project project)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
        }
    }
}
