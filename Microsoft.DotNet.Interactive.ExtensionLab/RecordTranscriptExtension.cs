using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class RecordTranscriptExtension : IKernelExtension
    {
        public Task OnLoadAsync(IKernel kernel)
        {
            if (kernel is CompositeKernel kernelBase)
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
                    kernelBase.AddMiddleware(async (command, context, next) =>
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

                kernelBase.AddDirective(record);
            }

            return Task.CompletedTask;
        }
    }
}