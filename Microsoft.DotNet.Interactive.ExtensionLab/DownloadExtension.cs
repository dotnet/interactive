// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DownloadExtension : IKernelExtension
    {
        public Task OnLoadAsync(IKernel kernel)
        {
            if (kernel is KernelBase kernelBase)
            {
                var download = new Command("#!download")
                {
                    new Option<Uri>("--uri"),
                    new Option<FileInfo>(
                        "--path",
                        parseArgument: ParsePath,
                        isDefault: true)
                };

                download.Handler = CommandHandler.Create(
                    (Uri uri, FileInfo path, KernelInvocationContext context) => Download(uri, path, context));

                kernelBase.AddDirective(download);
            }

            return Task.CompletedTask;

            FileInfo ParsePath(ArgumentResult argResult)
            {
                if (argResult.Tokens.Any())
                {
                    return new FileInfo(argResult.Tokens.Single().Value);
                }

                string fileName;

                if (argResult.Parent.Parent.Children["--uri"] is OptionResult uriResult && uriResult.GetValueOrDefault<Uri>() is { } fileUri)
                {
                    fileName = Path.GetFileName(fileUri.LocalPath);
                }
                else
                {
                    fileName = "downloaded";
                }

                return new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            }
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        private async Task Download(Uri uri, FileInfo path, KernelInvocationContext context)
        {

            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            path.Directory.EnsureExists();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(path.FullName);

            responseStream.CopyTo(fileStream);

            await context.DisplayAsync($"Created file: {path}");
        }
    }
}