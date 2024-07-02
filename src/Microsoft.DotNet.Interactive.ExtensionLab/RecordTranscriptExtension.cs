// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class RecordTranscriptExtension
{
    public static Task LoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            var record = new KernelActionDirective("#!record")
            {
                Description = "Records a replayable transcript of code submissions."
            };

            record.Parameters.Add(new KernelDirectiveParameter("--output")
            {
                Description = "The name of the file to write the transcript to",
                TypeHint = "file"
            });

            compositeKernel.AddDirective<StartCommandTranscription>(record, StartTranscription);

            Task StartTranscription(StartCommandTranscription command, KernelInvocationContext _)
            {
                compositeKernel.AddMiddleware(async (_, context, next) =>
                {
                    var json = KernelCommandEnvelope.Serialize(command);

                    await File.AppendAllLinesAsync(
                        command.OutputFile.FullName,
                        new[]
                        {
                            json
                        });

                    await next(command, context);
                });

                return Task.CompletedTask;
            }
        }

        KernelInvocationContext.Current?.Display(
            new HtmlString("""
                           <details><summary>Use the <code>#!record</code> magic command to keep a transcript of the code you run.</summary>
                               <p>Once you enable transcripts using <code>#!record</code>, each code submission (including re-running cells) is recorded in the specified file. The JSON format used is the same format recognized by the .NET Interactive <code>stdio</code> and <code>http</code> APIs and can be used to replay an interactive session via automation.</p>
                               <img src="https://user-images.githubusercontent.com/547415/109562409-343b1300-7a93-11eb-8ebf-79bb6af028cf.png" width="75%" />
                               </details>
                           """),
            "text/html");

        return Task.CompletedTask;
    }
}

public class StartCommandTranscription : KernelCommand
{
    public FileInfo OutputFile { get; set; }
}