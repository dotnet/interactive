// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal static class JupyterKernelExtensions
{
    public static async Task UseConfiguration(this JupyterKernel kernel, IJupyterKernelConfiguration configuration)
    {
        await configuration.ApplyAsync(kernel);
    }

    public static async Task RunOnKernelAsync(this JupyterKernel kernel, string code)
    {
        var result = await kernel.SendAsync(new SubmitCode(code));

        if (result.Events.OfType<CommandFailed>().SingleOrDefault() is { } failed)
        {
            throw new InvalidOperationException(failed.Message, failed.Exception);
        }
    }
}