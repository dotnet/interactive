// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class RecordTranscriptExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                var record = new Command(
                    "#!record",
                    "Records a replayable transcript of code submissions.")
                {
                    new Option<FileInfo>(
                        new [] {"-o", "--output"},
                        description: "The name of the file to write the transcript to")
                };

                record.Handler = CommandHandler.Create<FileInfo>(output =>
                {
                    compositeKernel.AddMiddleware(async (command, context, next) =>
                    {
                        var json = KernelCommandEnvelope.Serialize(command);

                        await File.AppendAllLinesAsync(
                            output.FullName,
                            new[]
                            {
                                json
                            });

                        await next(command, context);
                    });

                    return Task.CompletedTask;
                });

                compositeKernel.AddDirective(record);
            }

            return Task.CompletedTask;
        }
    }
}