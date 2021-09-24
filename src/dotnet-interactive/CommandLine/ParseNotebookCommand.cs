﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents.ParserServer;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    internal static class ParseNotebookCommand
    {
        public static Task Do(NotebookParserServer parserServer)
        {
            return parserServer.RunAsync();
        }
    }
}
