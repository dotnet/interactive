// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.ParserServer;
using Pocket;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.App.CommandLine;

internal static class ParseNotebookCommand
{
    public static Task RunParserServer(
        NotebookParserServer parserServer,
        DirectoryInfo logPath = null)
    {
        using var _ = Program.StartToolLogging(logPath);
        using var __ = Log.OnEnterAndExit();

        return parserServer.RunAsync();
    }
}