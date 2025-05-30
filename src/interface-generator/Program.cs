 // Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;

 namespace Microsoft.DotNet.Interactive.InterfaceGen.App; 

 class Program
 {
     static int Main(string[] args)
     {
         var outFileOption = new Option<FileInfo>("--out-file")
         {
             Description = "Location to write the generated interface file",
             Required = true
         }.AcceptExistingOnly();

         var command = new RootCommand
         {
             outFileOption
         };

         command.SetAction(async (parseResult, cancellationToken) => 
         {
             var generated = InterfaceGenerator.Generate();
             await File.WriteAllTextAsync(parseResult.GetValue(outFileOption).FullName, generated, cancellationToken);
             return 0;
         });

         return command.Parse(args).Invoke();
     }
 }