// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Tests.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class StdIOConnectionTests
    {
        private static CompositeKernel CreateCompositeKernel()
        {
            return new CompositeKernel().UseKernelClientConnection(new ConnectStdIoCommand());
        }

        [Theory]
        [InlineData(Language.CSharp, "<div class=\"dni-plaintext\">2</div>")]
        [InlineData(Language.FSharp, "<div class=\"dni-plaintext\">2</div>")]
        [InlineData(Language.PowerShell, "2")]
        public async Task dotnet_kernels_can_execute_code(Language language, string expected)
        {
            using var kernel = CreateCompositeKernel();

            var result = await kernel.SendAsync(new SubmitCode($"#!connect stdio --kernel-name proxy --command \"{Dotnet.Path}\" \"{typeof(App.Program).Assembly.Location}\" stdio --default-kernel {language.LanguageName()} --wait-for-kernel-ready-event true"));
            
            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();
            
            result = await kernel.SendAsync(new SubmitCode("1+1", targetKernelName: "proxy"));
            
            events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events
                .Should()
                .ContainSingle<DisplayEvent>(
                    @where: d => d.FormattedValues.Any(fv => fv.Value.Trim() == expected));
        }

        [Fact]
        public async Task stdio_server_encoding_is_utf_8()
        {
            using var kernel = CreateCompositeKernel();

            var result = await kernel.SendAsync(
                             new SubmitCode(
                                 $"#!connect stdio --kernel-name proxy --wait-for-kernel-ready-event true \"{Dotnet.Path}\" \"{typeof(Program).Assembly.Location}\" stdio --default-kernel csharp --log-path c:\\temp"));

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            result = await kernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName", "proxy"));

            events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();
            
            events
                .Should()
                .ContainSingle<DisplayEvent>(
                    @where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == $"{Encoding.UTF8.EncodingName}/{Encoding.UTF8.EncodingName}"));
        }

        [Fact]
        public async Task kernel_server_honors_log_path()
        {
            using var logPath = DisposableDirectory.Create();

            var waitTime = TimeSpan.FromSeconds(10);

            using (var kernel = CreateCompositeKernel())
            {
                var result = await kernel.SendAsync(
                                 new SubmitCode(
                                     $"#!connect stdio --kernel-name proxy --wait-for-kernel-ready-event \"{Dotnet.Path}\" \"{typeof(Program).Assembly.Location}\" stdio --log-path \"{logPath.Directory.FullName}\" --verbose "));

                result.KernelEvents.ToSubscribedList().Should().NotContainErrors();

                await kernel.SendAsync(new SubmitCode("1+1", "proxy"));
            }

            // wait for log file to be created
            var logFile = await logPath.Directory.WaitForFile(
                              timeout: waitTime,
                              predicate: _file => true); // any matching file is the one we want
            logFile.Should().NotBeNull($"a log file should have been created at {logFile.FullName}");

            // check log file for expected contents
            (await logFile.WaitForFileCondition(
                 timeout: waitTime,
                 predicate: file => file.Length > 0))
                .Should()
                .BeTrue($"expected non-empty log file within {waitTime.TotalSeconds}s");
            var logFileContents = await File.ReadAllTextAsync(logFile.FullName);
            logFileContents.Should().Contain("CodeSubmissionReceived: 1+1");
        }

        [Fact]
        public async Task fast_path_commands_over_proxy_can_be_handled()
        {
            var connector = new StdIoKernelConnector(new[]
            {
                Dotnet.Path.FullName,
                typeof(App.Program).Assembly.Location,
                "stdio",
                "--default-kernel",
                "csharp",
            });

            using var kernel = await connector.ConnectKernelAsync(new KernelName("proxy"));

            var markedCode = "var x = 12$$34;";

            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var column);

            var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, column)));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .EventuallyContainSingle<HoverTextProduced>();
        }
    }
}
