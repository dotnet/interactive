// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    public class StdioKernelTests
    {
        private async Task<TestStdioClient> CreateClient()
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "dotnet-interactive",
                Arguments = "stdio",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            var process = Process.Start(psi);
            var client = new TestStdioClient(process);
            await client.WaitForReady();
            return client;
        }

        [IntegrationTheory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task dotnet_kernels_can_execute_code(Language language)
        {
            using var client = await CreateClient();

            var events = client.Events.ToSubscribedList();

            client.SubmitCommand(new SubmitCode("1+1", targetKernelName: language.LanguageName()));

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(fv => fv.Value.Trim() == "<div class=\"dni-plaintext\">2</div>"),
                    timeout: 10_000);
        }
    }
}
