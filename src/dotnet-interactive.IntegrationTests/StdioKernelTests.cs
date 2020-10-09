// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        [InlineData(Language.CSharp, "<div class=\"dni-plaintext\">2</div>")]
        [InlineData(Language.FSharp, "<div class=\"dni-plaintext\">2</div>")]
        [InlineData(Language.PowerShell, "2")]
        public async Task dotnet_kernels_can_execute_code(Language language, string expected)
        {
            using var client = await CreateClient();

            var events = client.Events.ToSubscribedList();

            client.SubmitCommand(new SubmitCode("1+1", targetKernelName: language.LanguageName()));

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(fv => fv.Value.Trim() == expected),
                    timeout: 10_000);
        }

        [IntegrationFact]
        public async Task stdio_server_encoding_is_utf_8()
        {
            using var client = await CreateClient();

            var events = client.Events.ToSubscribedList();

            client.SubmitCommand(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName"));
            var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected),
                    timeout: 10_000);
        }
    }
}
