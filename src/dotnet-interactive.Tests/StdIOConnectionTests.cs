// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
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

            await kernel.SendAsync(new SubmitCode($"#!connect stdio --kernel-name proxy --command \"{Dotnet.Path}\" \"{typeof(App.Program).Assembly.Location}\" stdio --default-kernel {language.LanguageName()}"));

            var res = await kernel.SendAsync(new SubmitCode("1+1", targetKernelName: "proxy"));

            var events = res.KernelEvents.ToSubscribedList();

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(fv => fv.Value.Trim() == expected),
                    timeout: 10_000);
        }

        [Fact]
        public async Task stdio_server_encoding_is_utf_8()
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SendAsync(new SubmitCode($"#!connect stdio --kernel-name proxy --command \"{Dotnet.Path}\" \"{typeof(App.Program).Assembly.Location}\" stdio --default-kernel csharp"));
            
            var res = await kernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName", "proxy"));
            var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

            var events = res.KernelEvents.ToSubscribedList();

            events
                .Should()
                .EventuallyContainSingle<DisplayEvent>(
                    where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected),
                    timeout: 10_000);
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

            using var kernel = await connector.ConnectKernelAsync(new KernelInfo("proxy"));

            var markedCode = "var x = 12$$34;";

            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var column);

            var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, column)));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .EventuallyContainSingle<HoverTextProduced>();
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
        public async Task it_can_reuse_connection_for_multiple_proxy_kernel()
        {
           
            // setup connection

            using var connector = new StdIoKernelConnector(new[]
            {
                Dotnet.Path.FullName,
                typeof(App.Program).Assembly.Location,
                "stdio",
                "--default-kernel",
                "csharp",
            });

            // use same connection to create 2 proxy kernel

            using var localKernel1 = await connector.ConnectKernelAsync(new KernelInfo("kernel1"));

            using var localKernel2 = await connector.ConnectKernelAsync(new KernelInfo("kernel2"));

            var kernelCommand1 = new SubmitCode("\"echo1\"");

            var kernelCommand2 = new SubmitCode("\"echo2\"");

            var res1 = await localKernel1.SendAsync(kernelCommand1);

            var res2 = await localKernel2.SendAsync(kernelCommand2);

            var kernelEvents1 = res1.KernelEvents.ToSubscribedList();

            var kernelEvents2 = res2.KernelEvents.ToSubscribedList();

            kernelEvents1.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should()
                .Be(kernelCommand1.Code);

            kernelEvents1.Should().ContainSingle<ReturnValueProduced>().Which.FormattedValues.Should().ContainSingle(f => f.Value == "echo1");

            kernelEvents2.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should()
                .Be(kernelCommand2.Code);

            kernelEvents2.Should().ContainSingle<ReturnValueProduced>().Which.FormattedValues.Should().ContainSingle(f => f.Value == "echo2");
        }
    }
}
