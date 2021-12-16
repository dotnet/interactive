// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class VariableSharingTests
    {
        [Theory]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            @"using Microsoft.DotNet.Interactive;

(Kernel.Current.FindKernel(""fsharp"") as Microsoft.DotNet.Interactive.ValueSharing.ISupportGetValue).TryGetValue(""x"", out int x);
x")]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            "#!share --from fsharp x\nx")]
        [InlineData(
           "#!pwsh",
           "$x = 123",
           "#!share --from pwsh x\nx")]
        public async Task csharp_kernel_can_read_variables_from_other_kernels(
            string from,
            string codeToWrite,
            string codeToRead)
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"#!csharp\n{codeToRead}");

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be(123);
        }

        [Theory]
        [InlineData(
            "#!csharp",
            "var x = 123;",
            "#!share --from csharp x\nx")]
        [InlineData(
            "#!pwsh",
            "$x = 123",
            "#!share --from pwsh x\nx")]
        public async Task fsharp_kernel_can_read_variables_from_other_kernels(
            string from,
            string codeToWrite,
            string codeToRead)
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"#!fsharp\n{codeToRead}");

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be(123);
        }

        [Theory]
        [InlineData(
            "#!csharp",
            "var x = 123;",
            "#!share --from csharp x\n\"$($x):$($x.GetType().ToString())\"")]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            "#!share --from fsharp x\n\"$($x):$($x.GetType().ToString())\"")]
        public async Task pwsh_kernel_can_read_variables_from_other_kernels(
            string from,
            string codeToWrite,
            string codeToRead)
        {
            using var kernel = CreateKernel();
            
            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            var results = await kernel.SubmitCodeAsync($"#!pwsh\n{codeToRead}");

            results.KernelEvents.ToSubscribedList().Should()
                  .ContainSingle<StandardOutputValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
                  .Which
                  .Value
                  .Trim()
                  .Should()
                  .Be("123:System.Int32");
        }

        [Theory]
        [InlineData(
            "#!fsharp",
            "let x = 1",
            "#!share --from fsharp x")]
        [InlineData(
            "#!pwsh",
            "$x = 1",
            "#!share --from pwsh x")]
        public async Task csharp_kernel_variables_shared_from_other_kernels_resolve_to_the_correct_runtime_type(string from, string codeToWrite, string codeToRead)
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"#!csharp\n{codeToRead}\nx + 1");

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be(2);
        }

        [Theory]
        [InlineData(
            "#!csharp",
            "var x = 1;",
            "#!share --from csharp x")]
        [InlineData(
            "#!pwsh",
            "$x = 1",
            "#!share --from pwsh x")]
        public async Task fsharp_kernel_variables_shared_from_other_kernels_resolve_to_the_correct_runtime_types(string from, string codeToWrite, string codeToRead)
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"#!fsharp\n{codeToRead}\nx + 1");

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be(2);
        }

        [Theory]
        [InlineData(
            "#!csharp",
            "var x = 1;",
            "#!share --from csharp x")]
        [InlineData(
            "#!fsharp",
            "let x = 1",
            "#!share --from fsharp x")]
        public async Task pwsh_kernel_variables_shared_from_other_kernels_resolve_to_the_correct_runtime_type(string from, string codeToWrite, string codeToRead)
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"#!pwsh\n{codeToRead}\n$x + 1");

            events.Should()
                  .ContainSingle<StandardOutputValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
                  .Which
                  .Value
                  .Trim()
                  .Should()
                  .Be("2");
        }

        [Fact]
        public async Task javascript_ProxyKernel_can_share_a_value_from_csharp()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };
            kernel.DefaultKernelName = "csharp";
            using var remoteKernel = new FakeRemoteKernel();

            var receiver = new MultiplexingKernelCommandAndEventReceiver(remoteKernel.Receiver);

            using var host = new KernelHost(kernel, remoteKernel.Sender, receiver);

            var _ = host.ConnectAsync();

            var kernelInfo = new KernelInfo("javascript", null, new Uri("kernel://remote/js"));

            var javascriptKernel = await host.CreateProxyKernelOnDefaultConnectorAsync(kernelInfo);
            
            javascriptKernel.UseValueSharing(new JavaScriptKernelValueDeclarer());

            await kernel.SubmitCodeAsync("var csharpVariable = 123;");

            var submitCode = new SubmitCode(@"
#!javascript
#!share --from csharp csharpVariable");
            await kernel.SendAsync(submitCode);

            var remoteCommands = remoteKernel.Sender.Commands;

            remoteCommands.Should()
                          .ContainSingle<SubmitCode>()
                          .Which
                          .Code
                          .Should()
                          .Be("csharpVariable = 123;");
        }

        private static CompositeKernel CreateKernel()
        {
            return new CompositeKernel
            {
                new CSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseValueSharing(),
                new FSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseDefaultNamespaces() 
                    .UseValueSharing(),
                new PowerShellKernel()
                    .UseValueSharing()
            }.LogEventsToPocketLogger();
        }

        [Fact(Skip = "WIP")]
        public void Internal_types_are_shared_as_their_most_public_supertype()
        {
            throw new NotImplementedException("test not written");
        }
    }
}