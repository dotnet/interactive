// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.CSharpProject.Events
{
    public class ProjectOpened : KernelEvent
    {
        public IReadOnlyList<ProjectItem> ProjectItems { get; }

        public ProjectOpened(OpenProject command, IReadOnlyList<ProjectItem> projectItems)
            : base(command)
        {
            ProjectItems = projectItems;
        }
    }
}
