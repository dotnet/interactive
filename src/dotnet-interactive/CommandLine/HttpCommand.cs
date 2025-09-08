// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.CommandLine;

public static class HttpCommand
{
    public static Task<int> Do(
        StartupOptions startupOptions,
        CommandLineParser.StartWebServer startWebServer = null)
    {
        startWebServer?.Invoke(startupOptions);

        return Task.FromResult(0);
    }
}