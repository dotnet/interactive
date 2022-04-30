// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Pocket;
using Pocket.For.Xunit;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    [LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
    public class VariableSharingTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

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


            await kernel.SendAsync(new SubmitCode("",targetKernelName:"fsharp"));
            events.Should().NotContainErrors();



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

            var results = await kernel.SubmitCodeAsync($"#!pwsh\n{codeToRead} | Out-Display -MimeType text/plain");

            results.KernelEvents.ToSubscribedList().Should()
                  .ContainSingle<DisplayedValueProduced>()
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

            await kernel.SubmitCodeAsync($"#!pwsh\n{codeToRead}\n$x + 1 | Out-Display -MimeType text/plain");

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
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

            var remoteCommands = new List<KernelCommand>();

            remoteKernel.AddMiddleware(async (command, context, next) =>
            {
                remoteCommands.Add(command);
                await next(command, context);
            });

            await compositeKernel.SubmitCodeAsync("int csharpVariable = 123;");

            var submitCode = new SubmitCode(@"
#!javascript
#!share --from csharp csharpVariable");
            await compositeKernel.SendAsync(submitCode);

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
            var (compositeKernel, jsKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel();

            var jsVariableName = "jsVariable";

            jsKernel.RegisterCommandHandler<RequestValue>((cmd, context) =>
            {
                context.Publish(new ValueProduced(null, jsVariableName, new FormattedValue(JsonFormatter.MimeType, "123"), cmd));
                return Task.CompletedTask;
            });

            var result = await compositeKernel.SendAsync(new RequestKernelInfo("javascript"));

            var events = result.KernelEvents.ToSubscribedList();
            events.Should().NotContainErrors();

            var submitCode = new SubmitCode($@"
#!csharp
#!share --from javascript {jsVariableName}");
            await compositeKernel.SendAsync(submitCode);

            var csharpKernel = (CSharpKernel)compositeKernel.FindKernel("csharp");

            csharpKernel.GetValueInfos()
                        .Should()
                        .ContainSingle(v => v.Name == jsVariableName);

            csharpKernel.TryGetValue<double>(jsVariableName, out var jsVariable)
                        .Should().BeTrue();

            jsVariable.Should().Be(123);
        }

        [Fact]
        public async Task CSharpKernel_can_prompt_for_input_from_JavaScript_via_a_ProxyKernel()
        {
            var (compositeKernel, jsKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel();

            var valueKernel = new KeyValueStoreKernel().UseValueSharing();
            compositeKernel.Add(valueKernel);

            jsKernel.RegisterCommandHandler<RequestInput>((cmd, context) =>
            {
                context.Publish(new InputProduced("hello!", cmd));
                return Task.CompletedTask;
            });

            compositeKernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), "javascript");
            
            var valueName = "input";
          
            var submitCode = new SubmitCode($@"
#!value --name {valueName} --from-value @input:input-please
");
            var result = await compositeKernel.SendAsync(submitCode);

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            valueKernel
                .TryGetValue<string>(valueName, out var inputValue)
                .Should()
                .BeTrue();

            inputValue.Should().Be("hello!");
        }

        private async Task<(CompositeKernel, FakeKernel)> CreateCompositeKernelWithJavaScriptProxyKernel()
        {
            var localCompositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseValueSharing()
            };
            localCompositeKernel.DefaultKernelName = "csharp";

            var remoteCompositeKernel = new CompositeKernel();
            var remoteKernel = new FakeKernel("remote-javascript");
            remoteCompositeKernel.Add(remoteKernel);

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            var remoteKernelUri = new Uri("kernel://remote/remote-javascript");
            var javascriptKernel =
                await localCompositeKernel
                      .Host
                      .ConnectProxyKernelOnDefaultConnectorAsync(
                          "javascript",
                          remoteKernelUri);

            await localCompositeKernel.SendAsync(new RequestKernelInfo(remoteKernelUri));

            javascriptKernel.UseValueSharing(new JavaScriptValueDeclarer());

            _disposables.Add(remoteCompositeKernel);

            return (localCompositeKernel, remoteKernel);
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