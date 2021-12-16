// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;

namespace Microsoft.DotNet.Interactive.InterfaceGen.App
{
    class Program
    {
        static int Main(string[] args)
        {
            var existingOnlyOption = new Option<FileInfo>("--out-file")
            {
                Description = "Location to write the generated interface file",
                IsRequired = true
            }.ExistingOnly();

            var command = new RootCommand
            {
                existingOnlyOption
            };

            command.SetHandler(async (FileInfo f) =>
            {
                var generated = InterfaceGenerator.Generate();
                await File.WriteAllTextAsync(f.FullName, generated);
            }, existingOnlyOption);

            return command.Invoke(args);
        }
    }
}