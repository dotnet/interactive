// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal static class JupyterKernelExtensions
{
    public async static Task UseConfiguration(this JupyterKernel kernel, IJupyterKernelConfiguration configuration)
    {
        await configuration.ApplyAsync(kernel);
    }

    public async static Task<bool> RunOnKernelAsync(this JupyterKernel kernel, string code)
    {
        var results = await kernel.SendAsync(new SubmitCode(code));
        var success = results.Events.OfType<CommandSucceeded>().FirstOrDefault();

        return success is { };
    }
}
