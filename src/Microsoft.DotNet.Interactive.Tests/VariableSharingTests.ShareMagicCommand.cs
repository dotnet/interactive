// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;

namespace Microsoft.DotNet.Interactive.Tests;

public partial class VariableSharingTests
{
    [TestClass]
    public class ShareMagicCommand : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        [TestMethod]
        [DataRow(
            "#!fsharp",
            "let x = 123",
            "#!share --from fsharp x\nx")]
        [DataRow(
            "#!pwsh",
            "$x = 123",
            "#!share --from pwsh x\nx")]
        public async Task csharp_kernel_can_read_variables_from_other_kernels(
            string from,
            string codeToWrite,
            string codeToRead)
        {
            using var kernel = CreateCompositeKernel();

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

        [TestMethod]
        [DataRow(
            "#!csharp",
            "var x = 123;",
            "#!share --from csharp x\nx")]
        [DataRow(
            "#!pwsh",
            "$x = 123",
            "#!share --from pwsh x\nx")]
        public async Task fsharp_kernel_can_read_variables_from_other_kernels(
            string from,
            string codeToWrite,
            string codeToRead)
        {
            using var kernel = CreateCompositeKernel();

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

        [TestMethod]
        [DataRow(
            "#!csharp",
            "var x = 123;",
            "#!share --from csharp x\n\"$($x):$($x.GetType().ToString())\"")]
        [DataRow(
            "#!fsharp",
            "let x = 123",
            "#!share --from fsharp x\n\"$($x):$($x.GetType().ToString())\"")]
        public async Task pwsh_kernel_can_read_variables_from_other_kernels(
            string from,
            string codeToWrite,
            string codeToRead)
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            var results = await kernel.SubmitCodeAsync($"#!pwsh\n{codeToRead} | Out-Display -MimeType text/plain");

            results.Events.Should()
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

        [TestMethod]
        [DataRow(
            "#!fsharp",
            "let x = 1",
            "#!share --from fsharp x")]
        [DataRow(
            "#!pwsh",
            "$x = 1",
            "#!share --from pwsh x")]
        public async Task csharp_kernel_variables_shared_from_other_kernels_resolve_to_the_correct_runtime_type(string from, string codeToWrite, string codeToRead)
        {
            using var kernel = CreateCompositeKernel();

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

        [TestMethod]
        [DataRow(
            "#!csharp",
            "var x = 1;",
            "#!share --from csharp x")]
        [DataRow(
            "#!pwsh",
            "$x = 1",
            "#!share --from pwsh x")]
        public async Task fsharp_kernel_variables_shared_from_other_kernels_resolve_to_the_correct_runtime_types(string from, string codeToWrite, string codeToRead)
        {
            using var kernel = CreateCompositeKernel();

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

        [TestMethod]
        [DataRow(
            "#!csharp",
            "var x = 1;",
            "#!share --from csharp x")]
        [DataRow(
            "#!fsharp",
            "let x = 1",
            "#!share --from fsharp x")]
        public async Task pwsh_kernel_variables_shared_from_other_kernels_resolve_to_the_correct_runtime_type(string from, string codeToWrite, string codeToRead)
        {
            using var kernel = CreateCompositeKernel();

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

        [TestMethod]
        public async Task JavaScript_ProxyKernel_can_share_a_value_from_csharp()
        {
            var (compositeKernel, remoteKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel(_disposables);

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

            result.Events.Should().NotContainErrors();

            remoteCommands.Should()
                          .ContainSingle<SendValue>()
                          .Which
                          .FormattedValue.Value
                          .Should()
                          .Be("123");
        }

        [TestMethod]
        public async Task CSharpKernel_can_share_variable_from_JavaScript_via_a_ProxyKernel()
        {
            var (compositeKernel, jsKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel(_disposables);

            var jsVariableName = "jsVariable";

            jsKernel.RegisterCommandHandler<RequestValue>((cmd, context) =>
            {
                context.Publish(new ValueProduced(123, jsVariableName, new FormattedValue(JsonFormatter.MimeType, "123"), cmd));
                return Task.CompletedTask;
            });

            var result = await compositeKernel.SendAsync(new RequestKernelInfo("javascript"));

            result.Events.Should().NotContainErrors();

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

        [TestMethod]
        public async Task CSharpKernel_can_prompt_for_input_from_JavaScript_via_a_ProxyKernel()
        {
            var (compositeKernel, jsKernel) = await CreateCompositeKernelWithJavaScriptProxyKernel(_disposables);

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

            result.Events.Should().NotContainErrors();

            var valueProduced = await valueKernel.RequestValueAsync(valueName);
            
            valueProduced.FormattedValue.Value.Should().Be("hello!");
        }

        [TestMethod]
        public async Task Values_can_be_shared_using_a_specified_MIME_type()
        {
            using var kernel = CreateCompositeKernel();

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

        [TestMethod]
        public async Task When_plaintext_MIME_type_is_specified_then_a_string_is_declared()
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SubmitCodeAsync("#!csharp\nvar stringType = typeof(string);");

            var result = await kernel.SubmitCodeAsync(@"
#!fsharp
#!share --from csharp stringType --mime-type text/plain
stringType");

            result.Events.Should().NotContainErrors();

            result.Events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be("System.String");
        }

        [TestMethod]
        public async Task honors_mimetype_from_value_kernel()
        {
            var csharpKernel = CreateKernel(Language.CSharp);
            var valueKernel = new KeyValueStoreKernel().UseValueSharing();

            using var composite = new CompositeKernel
            {
                csharpKernel,
                valueKernel
            };
            var jsonFragment = @"{
    ""a"" : 123
}";
            await composite.SendAsync(new SubmitCode(@$"#!value --name data --mime-type application/json
{jsonFragment}
"));

            var result = await composite.SendAsync(new SubmitCode("#!share --from value --as x data ", targetKernelName: csharpKernel.Name));

            result.Events.Should().NotContainErrors();
            var valueProduced = await csharpKernel.RequestValueAsync("x");

            var expected = JsonDocument.Parse(jsonFragment);
            valueProduced.Value.Should()
                         .BeOfType<JsonDocument>()
                         .Which
                         .Should()
                         .BeEquivalentTo(expected, opt => opt.ComparingByMembers<JsonElement>());
        }

        [TestMethod]
        public async Task A_name_can_be_specified_for_the_imported_value()
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SubmitCodeAsync("#!csharp\nvar x = 123;");

            var result = await kernel.SubmitCodeAsync("""
                #!fsharp
                #!share --from csharp x --mime-type text/plain --as y
                y
                """);

            result.Events.Should().NotContainErrors();

            result.Events.Should()
                  .ContainSingle<ValueProduced>()
                  .Which
                  .FormattedValue
                  .Should()
                  .BeEquivalentTo(new FormattedValue("text/plain", "123"));
        }

        [TestMethod]
        [DataRow(Language.CSharp)]
        [DataRow(Language.FSharp)]
        [DataRow(Language.PowerShell)]
        public async Task RequestValue_returns_defined_variable(Language language)
        {
            var codeToSetVariable = language switch
            {
                Language.CSharp => "var x = 123;",
                Language.FSharp => "let x = 123",
                Language.PowerShell => "$x = 123"
            };

            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(codeToSetVariable);

            var valueProduced = await kernel.RequestValueAsync("x");

            valueProduced.Value.Should().Be(123);
        }

        [TestMethod]
        [DataRow(Language.CSharp)]
        [DataRow(Language.FSharp)]
        [DataRow(Language.PowerShell)]
        public async Task RequestValueInfos_returns_the_names_of_defined_variables(Language language)
        {
            var codeToSetVariable = language switch
            {
                Language.CSharp => "var x = 123;",
                Language.FSharp => "let x = 123",
                Language.PowerShell => "$x = 123"
            };

            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(codeToSetVariable);

            var (success, valueInfosProduced) = await kernel.TryRequestValueInfosAsync();

            valueInfosProduced.ValueInfos.Should().Contain(v => v.Name == "x");
        }

        [TestMethod]
        public async Task RequestValueInfos_shows_expected_type_name()
        {
            var kernel = CreateKernel(Language.CSharp);

            await kernel.SubmitCodeAsync("var x = new List<string>();");

            var (success, valueInfosProduced) = await kernel.TryRequestValueInfosAsync();

            valueInfosProduced.ValueInfos
                              .Should()
                              .Contain(v => v.TypeName == "System.Collections.Generic.List<System.String>");
        }

        [TestMethod]
        [DataRow(Language.CSharp)]
        [DataRow(Language.FSharp)]
        [DataRow(Language.PowerShell)]
        public async Task SendValue_declares_the_specified_variable(Language language)
        {
            using var kernel = CreateKernel(language);

            await kernel.SendAsync(new SendValue("x", 123));

            var valueProduced = await kernel.RequestValueAsync("x");

            valueProduced.Value.Should().Be(123);
        }

        [TestMethod]
        [DataRow(Language.CSharp)]
        [DataRow(Language.FSharp)]
        [DataRow(Language.PowerShell)]
        public async Task SendValue_overwrites_an_existing_variable_of_the_same_type(Language language)
        {
            using var kernel = CreateKernel(language);

            await kernel.SendAsync(new SendValue("x", 123));
            await kernel.SendAsync(new SendValue("x", 456));

            var valueProduced = await kernel.RequestValueAsync("x");

            valueProduced.Value.Should().Be(456);
        }

        [TestMethod]
        [DataRow(Language.CSharp)]
        [DataRow(Language.FSharp)]
        [DataRow(Language.PowerShell)]
        public async Task SendValue_can_redeclare_an_existing_variable_and_change_its_type(Language language)
        {
            using var kernel = CreateKernel(language);

            await kernel.SendAsync(new SendValue("x", 123));
            await kernel.SendAsync(new SendValue("x", "hello"));

            var valueProduced = await kernel.RequestValueAsync("x");

            valueProduced.Value.Should().Be("hello");
        }

        [TestMethod]
        public async Task FSharp_can_set_an_array_value_with_SendValue()
        {
            using var kernel = CreateKernel(Language.FSharp);

            await kernel.SendAsync(new SendValue("x", new int[] { 42 }));

            var valueProduced = await kernel.RequestValueAsync("x");

            valueProduced.Value.Should().BeEquivalentTo(new int[] { 42 });
        }

        private static CompositeKernel CreateCompositeKernel()
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
                        .UseValueSharing(),
            };
        }

        public void Dispose() => _disposables.Dispose();
    }

    internal static async Task<(CompositeKernel, FakeKernel)> CreateCompositeKernelWithJavaScriptProxyKernel(CompositeDisposable disposables)
    {
        var localCompositeKernel = new CompositeKernel
        {
            new CSharpKernel().UseValueSharing()
        };
        localCompositeKernel.DefaultKernelName = "csharp";

        var remoteCompositeKernel = new CompositeKernel();
        var remoteKernel = new FakeKernel("remote-javascript", "javascript");
        remoteKernel.RegisterCommandType<RequestValue>();
        remoteKernel.RegisterCommandType<RequestValueInfos>();
        remoteKernel.RegisterCommandType<SendValue>();

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

        javascriptKernel.UseValueSharing();

        await localCompositeKernel.SendAsync(new RequestKernelInfo(remoteKernelUri));

        disposables.Add(localCompositeKernel);
        disposables.Add(remoteCompositeKernel);

        return (localCompositeKernel, remoteKernel);
    }
}