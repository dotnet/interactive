// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.CommandLine;

internal static class KernelHostLauncher
{
    public static async Task<int> Do(StartupOptions startupOptions, KernelHost kernelHost, IConsole console)
    {
        var disposable = Program.StartToolLogging(startupOptions);
        await kernelHost.ConnectAndWaitAsync();
        disposable.Dispose();
        return 0;
    }
}