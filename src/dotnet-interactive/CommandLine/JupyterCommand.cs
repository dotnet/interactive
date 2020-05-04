// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public static class JupyterCommand
    {
        public static Task<int> Do(
            StartupOptions startupOptions,
            IConsole console,
            CommandLineParser.StartServer startServer = null,
            InvocationContext context = null)
        {
            startServer?.Invoke(startupOptions, context);

            return Task.FromResult(0);
        }
    }
}