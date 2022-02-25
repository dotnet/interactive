// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class VariableSharingTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

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
            using var _ = await ConsoleLock.AcquireAsync();

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
            using var _ = await ConsoleLock.AcquireAsync();

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
        public async Task JavaScript_ProxyKernel_can_share_a_value_from_csharp()
        {
            var (compositeKernel, remoteKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel();

            await compositeKernel.SubmitCodeAsync("var csharpVariable = 123;");

            var submitCode = new SubmitCode(@"
#!javascript
#!share --from csharp csharpVariable");
            await compositeKernel.SendAsync(submitCode);

            var remoteCommands = remoteKernel.Sender.Commands;

            remoteCommands.Should()
                          .ContainSingle<SubmitCode>()
                          .Which
                          .Code
                          .Should()
                          .Be("csharpVariable = 123;");
        }

        [Fact]
        public async Task CSharpKernel_can_share_variable_from_JavaScript_via_a_ProxyKernel()
        {
            var (compositeKernel, remoteKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel();
            
            remoteKernel.RegisterCommandHandler<RequestValue>((cmd, context) =>
            {
                context.Publish(new ValueProduced(null, "jsVariable", new FormattedValue(JsonFormatter.MimeType, "[1, 2, 3]"), cmd));
                return Task.CompletedTask;
            });

            var jsVariableName = @"jsVariable";

            var submitCode = new SubmitCode($@"
#!csharp
#!share --from javascript {jsVariableName}");
            await compositeKernel.SendAsync(submitCode);

            var csharpKernel = (CSharpKernel) compositeKernel.FindKernel("csharp");

            csharpKernel.GetValueInfos()
                        .Should()
                        .ContainSingle(v => v.Name == jsVariableName);

            csharpKernel.TryGetValue<int[]>(jsVariableName, out var jsVariable);





            // TODO (CSharpKernel_can_share_variable_fro_JavaScript_ProxyKernel) write test
            throw new NotImplementedException();
        }

        private async Task<(CompositeKernel, FakeRemoteKernel)> CreateCompositeKernelWithJavaScriptProxyKernel()
        {
            var compositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseValueSharing()
            };
            compositeKernel.DefaultKernelName = "csharp";

            var remoteKernel = new FakeRemoteKernel();

            var receiver = new MultiplexingKernelCommandAndEventReceiver(remoteKernel.Receiver);

            var host = compositeKernel.UseHost(remoteKernel.Sender, receiver);

            var _ = host.ConnectAsync();

            var kernelInfo = new KernelInfo("javascript")
            {
                DestinationUri = new("kernel://remote/js")
            };

            var javascriptKernel = await host.CreateProxyKernelOnDefaultConnectorAsync(kernelInfo);

            javascriptKernel.UseValueSharing(new JavaScriptKernelValueDeclarer());

            _disposables.Add(remoteKernel);
            _disposables.Add(host);

            return (compositeKernel, remoteKernel);
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

        public void Dispose() => _disposables.Dispose();
    }
}