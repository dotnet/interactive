// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands;

public class ChangeWorkingDirectory : KernelCommand
{
    public string WorkingDirectory { get; }

    public ChangeWorkingDirectory(string workingDirectory)
    {
        WorkingDirectory = workingDirectory;
    }

    public override Task InvokeAsync(KernelInvocationContext context)
    {
        var currentWorkingDir = Directory.GetCurrentDirectory();

        if (!currentWorkingDir.Equals(WorkingDirectory, StringComparison.Ordinal))
        {
            Directory.SetCurrentDirectory(WorkingDirectory);

            context.Publish(new WorkingDirectoryChanged(WorkingDirectory, this));
        }

        return Handler(this, context);
    }
}