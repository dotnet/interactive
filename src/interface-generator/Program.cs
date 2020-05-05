// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Microsoft.DotNet.Interactive
{
    class Program
    {
        static int Main(string[] args)
        {
            var command = new RootCommand()
            {
                new Option<FileInfo>("--out-file")
                {
                    Description = "Location to write the generated interface file",
                    Required = true
                }
            };
            command.Handler = CommandHandler.Create((FileInfo outFile) =>
            {
                var generated = InterfaceGenerator.Generate();
                File.WriteAllText(outFile.FullName, generated);
            });
            return command.Invoke(args);
        }
    }
}
