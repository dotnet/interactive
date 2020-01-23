// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public class StartupOptions
    {
        public static StartupOptions FromCommandLine(string commandLine)
        {
            StartupOptions startupOptions = null;

            CommandLineParser.Create(new ServiceCollection(), startServer: (options, context) => { startupOptions = options; })
                             .InvokeAsync(commandLine);

            return startupOptions;
        }

        public StartupOptions(
            DirectoryInfo logPath = null,
            bool verbose = false)
        {
            LogPath = logPath;
            Verbose = verbose;
        }

        public DirectoryInfo LogPath { get; }

        public bool Verbose { get; }
    }
}