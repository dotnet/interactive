// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
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
            var result = await compositeKernel.SendAsync(submitCode);

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            remoteCommands.Should()
                          .ContainSingle<SendValue>()
                          .Which
                          .Name
                          .Should()
                          .Be("csharpVariable");

            // FIX: (JavaScript_ProxyKernel_can_share_a_value_from_csharp) should this still use the codegen approach or should it rely on the JS kernel to do that part?
            throw new NotImplementedException();
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

            var csharpKernel = (CSharpKernel)compositeKernel.FindKernelByName("csharp");

            var (success, valueInfosProduced) = await csharpKernel.TryRequestValueInfosAsync();
            valueInfosProduced.ValueInfos
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

            var (success, valueProduced) = await valueKernel.TryRequestValueAsync(valueName);

            success
                .Should()
                .BeTrue();

            valueProduced.Value.Should().Be("hello!");
        }

        [Fact]
        public async Task Values_can_be_shared_using_a_specified_MIME_type()
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!csharp\nvar x = 123;");

            await kernel.SubmitCodeAsync(@"
#!fsharp
#!share --from csharp x --mime-type text/html
x");
            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<ValueProduced>()
                  .Which
                  .FormattedValue
                  .Should()
                  .BeEquivalentTo(new FormattedValue("text/html", 123.ToDisplayString("text/html")));
        }

        [Fact]
        public async Task When_a_MIME_type_is_specified_then_a_string_is_declared_instead_of_a_reference()
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!csharp\nvar stringType = typeof(string);");

            await kernel.SubmitCodeAsync(@"
#!fsharp
#!share --from csharp stringType --mime-type text/plain
stringType");

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be("System.String");
        }

        [Fact]
        public async Task A_name_can_be_specified_for_the_imported_value()
        {
            using var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!csharp\nvar x = 123;");

            await kernel.SubmitCodeAsync(@"
#!fsharp
#!share --from csharp x --mime-type text/plain --as y
y");

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<ValueProduced>()
                  .Which
                  .FormattedValue
                  .Should()
                  .BeEquivalentTo(new FormattedValue("text/plain", 123.ToDisplayString("text/plain")));
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
            
            javascriptKernel.UseValueSharing();

            _disposables.Add(localCompositeKernel);
            _disposables.Add(remoteCompositeKernel);

            return (localCompositeKernel, remoteKernel);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task SendValue_declares_the_specified_variable(Language language)
        {
            using var kernel = CreateKernel(language);

            await kernel.SendAsync(new SendValue("x", 123));

            var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            valueProduced.Value.Should().Be(123);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task SendValue_overwrites_an_existing_variable_of_the_same_type(Language language)
        {
            using var kernel = CreateKernel(language);

            await kernel.SendAsync(new SendValue("x", 123));
            await kernel.SendAsync(new SendValue("x", 456));

            var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            valueProduced.Value.Should().Be(456);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task SendValue_can_redeclare_an_existing_variable_and_change_its_type(Language language)
        {
            using var kernel = CreateKernel(language);

            await kernel.SendAsync(new SendValue("x", 123));
            await kernel.SendAsync(new SendValue("x", "hello"));

            var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            valueProduced.Value.Should().Be("hello");
        }

        [Fact]
        public async Task FSharp_can_set_an_array_value_with_SendValue()
        {
            using var kernel = CreateKernel(Language.FSharp);

            await kernel.SendAsync(new SendValue("x", new int[] { 42 }));

            var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            valueProduced.Value.Should().BeEquivalentTo(new int[] { 42 });
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

        private static Kernel CreateKernel(Language language)
        {
            return language switch
            {
                Language.CSharp =>
                    new CSharpKernel()
                        .UseNugetDirective()
                        .UseKernelHelpers()
                        .UseValueSharing(),
                Language.FSharp =>
                    new FSharpKernel()
                        .UseNugetDirective()
                        .UseKernelHelpers()
                        .UseValueSharing(),
                Language.PowerShell =>
                    new PowerShellKernel()
                        .UseValueSharing()
            };
        }

        public void Dispose() => _disposables.Dispose();
    }
}