﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
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

            KernelInvocationContext.Current?.Display(
                new HtmlString(@"<details><summary>Use the <code>#!record</code> magic command to keep a transcript of the code you run.</summary>
    <p>Once you enable transcripts using <code>#!record</code>, each code submission (including re-running cells) is recorded in the specified file. The JSON format used is the same format recognized by the .NET Interactive <code>stdio</code> and <code>http</code> APIs and can be used to replay a interactive session via automation.</p>
    <img src=""https://user-images.githubusercontent.com/547415/109562409-343b1300-7a93-11eb-8ebf-79bb6af028cf.png"" width=""75%"" />
    </details>"),
                "text/html");

            return Task.CompletedTask;
        }
    }
}