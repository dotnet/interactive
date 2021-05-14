// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    public class StdioKernelTests
    {
        private async Task<Kernel> CreateProxyKernel(Language language)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet-interactive",
                Arguments = $"stdio --default-kernel {language.LanguageName()}",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var process = new Process { StartInfo = psi };
            TaskCompletionSource<bool> ready = new();
            process.Start();

            var receiver = new KernelCommandAndEventTextStreamReceiver(process.StandardOutput);
            var sender = new KernelCommandAndEventTextStreamSender(process.StandardInput);
            
            var kernel = new ProxyKernel2("proxy", receiver, sender);
            
            kernel.RegisterForDisposal(() =>
            {
                process.Kill(true);
                process.Dispose();
            });

            var _ = kernel.RunAsync();

            var sub = kernel.KernelEvents.OfType<KernelReady>().Subscribe(_ =>
            {
                ready.SetResult(true);
            });

            await ready.Task;
            sub.Dispose();

            return kernel;
        }

        [IntegrationTheory]
        [InlineData(Language.CSharp, "<div class=\"dni-plaintext\">2</div>")]
        [InlineData(Language.FSharp, "<div class=\"dni-plaintext\">2</div>")]
        [InlineData(Language.PowerShell, "2")]
        public async Task dotnet_kernels_can_execute_code(Language language, string expected)
        {
            using var kernel = await CreateProxyKernel(language);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode("1+1", targetKernelName: kernel.Name));

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(fv => fv.Value.Trim() == expected),
                    timeout: 10_000);
        }

        [IntegrationFact]
        public async Task stdio_server_encoding_is_utf_8()
        {
            using var kernel = await CreateProxyKernel(Language.CSharp);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName", kernel.Name));
            var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected),
                    timeout: 10_000);
        }
    }
}
