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
