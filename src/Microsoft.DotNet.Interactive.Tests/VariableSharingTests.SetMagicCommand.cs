// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public partial class VariableSharingTests
{
    public class SetMagicCommand : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        [Fact]
        public async Task can_set_value_prompting_user()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var composite = new CompositeKernel();

            composite.Add(kernel);

            composite.RegisterCommandHandler<RequestInput>((requestInput, context) =>
            {
                context.Publish(new InputProduced("hello!", requestInput));
                return Task.CompletedTask;
            });

            composite.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), composite.Name);


            await composite.SendAsync(new SubmitCode("#!set --name x --value @input:input-please"));
            var valueProduced = await kernel.RequestValueAsync("x");
            
            valueProduced.Value.Should().BeEquivalentTo("hello!");
        }

        [Theory]
        [InlineData(
            """
                #!fsharp
                let x = 123
                """,
            """
                #!csharp
                #!set --name x --value @fsharp:x
                x
                """)]
        [InlineData(
            """
                #!pwsh
                $x = 123
                """,
            """
                #!csharp
                #!set --name x --value @pwsh:x
                x
                """)]
        public async Task csharp_kernel_can_read_variables_from_other_kernels(
            string sourceCode,
            string destinationCode)
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SubmitCodeAsync(sourceCode);

            var result = await kernel.SubmitCodeAsync(destinationCode);

            result.Events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be(123);
        }

        [Theory]
        [InlineData(
            """
                #!csharp
                var x = 123;
                """,
            """
                #!fsharp
                #!set --name x --value @csharp:x
                x
                """)]
        [InlineData(
            """
                #!pwsh
                $x = 123
                """,
            """
                #!fsharp
                #!set --name x --value @pwsh:x
                x
                """)]
        public async Task fsharp_kernel_can_read_variables_from_other_kernels(
            string sourceCode,
            string destinationCode)
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SubmitCodeAsync(sourceCode);

            var result = await kernel.SubmitCodeAsync(destinationCode);

            result.Events.Should()
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
            """
                #!set --name x --value @csharp:x
                "$($x):$($x.GetType().ToString())"
                """)]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            """
                #!set --name x --value @fsharp:x
                "$($x):$($x.GetType().ToString())"
                """)]
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
                   .Be("123:System.Double");
        }

        [Fact]
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
#!set --value @csharp:csharpVariable --name csharpVariable");
            var result = await compositeKernel.SendAsync(submitCode);

            result.Events.Should().NotContainErrors();

            remoteCommands.Should()
                          .ContainSingle<SendValue>()
                          .Which
                          .FormattedValue.Value
                          .Should()
                          .Be("123");
        }

        [Theory]
        [InlineData("let source = 456", 456)]
        [InlineData("let source = \"hello\"", "hello")]
        [InlineData("let source = true", true)]
        public async Task can_set_value_from_another_kernel_when_serialized_as_scalar(
            string sourceDeclaration,
            object expectedDestinationValue)
        {
            using var composite = CreateCompositeKernel();

            await composite.FindKernelByName("fsharp").SendAsync(new SubmitCode(sourceDeclaration));

            await composite.SendAsync(new SubmitCode("#!set --name destination --value @fsharp:source", targetKernelName: "csharp"));

            var valueProduced = await composite.FindKernelByName("csharp").RequestValueAsync("destination");

            valueProduced.Value.Should().Be(expectedDestinationValue);
        }

        [Fact]
        public async Task can_set_value_from_another_kernel_when_serialized_as_array()
        {
            using var composite = CreateCompositeKernel();

            await composite.FindKernelByName("fsharp").SendAsync(new SubmitCode("""
                let source = [1;2;3]
                """));

            await composite.SendAsync(new SubmitCode("#!set --name destination --value @fsharp:source", targetKernelName: "csharp"));

            var valueProduced = await composite.FindKernelByName("csharp").RequestValueAsync("destination");

            valueProduced.Value.Should()
                         .BeOfType<JsonDocument>()
                         .Which
                         .Deserialize<int[]>()
                         .Should()
                         .BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public async Task can_set_value_from_another_kernel_when_serialized_as_object()
        {
            using var composite = CreateCompositeKernel();

            await composite.FindKernelByName("fsharp").SendAsync(new SubmitCode("""
                let source = System.Drawing.Point(1,2)
                """));

            await composite.SendAsync(new SubmitCode("#!set --name destination --value @fsharp:source", targetKernelName: "csharp"));

            var valueProduced = await composite.FindKernelByName("csharp").RequestValueAsync("destination");

            valueProduced.Value.Should()
                         .BeOfType<JsonDocument>()
                         .Which
                         .Deserialize<Point>()
                         .Should()
                         .BeEquivalentTo(new { X = 1, Y = 2 });
        }

        [Fact]
        public async Task Values_are_not_shared_by_reference_by_default()
        {
            using var kernel = CreateCompositeKernel();

            await kernel.FindKernelByName("fsharp")
                        .SubmitCodeAsync("let list = System.Collections.Generic.List<int>()");

            await kernel
                  .FindKernelByName("csharp")
                  .SubmitCodeAsync("using System.IO;");

            var result = await kernel
                               .FindKernelByName("csharp")
                               .SubmitCodeAsync("""
                               #!set --value @fsharp:list --name list
                               list
                               """);

            result.Events.Should().NotContainErrors();

            result.Events
                  .Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .BeOfType<JsonDocument>();
        }

        [Fact]
        public async Task Byref_sharing_is_not_allowed_when_source_kernel_is_a_proxy()
        {
            var (compositeKernel, _) = await CreateCompositeKernelWithJavaScriptProxyKernel(_disposables);

            await compositeKernel.SubmitCodeAsync("int csharpVariable = 123;");

            var submitCode = new SubmitCode("""
                #!set --value @csharp:csharpVariable --name x --byref
                """);

            var result = await compositeKernel
                               .FindKernelByName("javascript")
                               .SendAsync(submitCode);

            result.Events.Last()
                  .Should()
                  .BeOfType<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .StartWith("Sharing by reference is not allowed when kernels are remote.");
        }

        [Fact]
        public async Task Byref_sharing_is_not_allowed_when_destination_kernel_is_a_proxy()
        {
            var (compositeKernel, _) = await CreateCompositeKernelWithJavaScriptProxyKernel(_disposables);

            var submitCode = new SubmitCode("""
                #!set --value @javascript:jsVariable --name x --byref
                """);

            var result = await compositeKernel
                               .FindKernelByName("csharp")
                               .SendAsync(submitCode);

            result.Events.Last()
                  .Should()
                  .BeOfType<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("Sharing by reference is not allowed when kernels are remote.");
        }

        [Fact]
        public async Task Values_can_be_shared_by_reference_using_the_byref_option()
        {
            using var kernel = CreateCompositeKernel();

            await kernel.FindKernelByName("fsharp")
                        .SubmitCodeAsync("""let dir = System.IO.DirectoryInfo(".")""");

            await kernel
                  .FindKernelByName("csharp")
                  .SubmitCodeAsync("using System.IO;");

            var result = await kernel
                               .FindKernelByName("csharp")
                               .SubmitCodeAsync("""
                               #!set --value @fsharp:dir --byref --name dir
                               dir
                               """);

            result.Events.Should().NotContainErrors();

            result.Events
                  .Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .BeOfType<DirectoryInfo>();
        }

        [Fact]
        public async Task the_byref_option_cannot_be_combined_with_the_MIME_type_option()
        {
            using var kernel = CreateKernel(Language.CSharp);

            var result = await kernel.SubmitCodeAsync("#!set --name dir --value @fsharp:dir --byref --mime-type text/plain");

            result.Events
                  .Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Contain("The --mime-type and --byref options cannot be used together.");
        }

        [Fact]
        public async Task When_plaintext_MIME_type_is_specified_then_a_string_is_declared()
        {
            using var kernel = CreateCompositeKernel();

            await kernel.SubmitCodeAsync("""
                #!csharp
                var stringType = typeof(string);
                """);

            var result = await kernel.SubmitCodeAsync("""
                
                #!fsharp
                #!set --name stringType --mime-type text/plain --value @csharp:stringType
                stringType
                """);

            result.Events.Should().NotContainErrors();

            result.Events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be("System.String");
        }

        [Fact]
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

            var result = await composite.SendAsync(new SubmitCode($"#!set --name x --value @{valueKernel.Name}:data ", targetKernelName: csharpKernel.Name));

            result.Events.Should().NotContainErrors();
            var valueProduced = await csharpKernel.RequestValueAsync("x");


            var expected = JsonDocument.Parse(jsonFragment);
            valueProduced.Value.Should()
                         .BeOfType<JsonDocument>()
                         .Which
                         .Should()
                         .BeEquivalentTo(expected, opt => opt.ComparingByMembers<JsonElement>());
        }

        [Fact]
        public async Task name_option_is_required()
        {
            using var kernel = CreateKernel(Language.CSharp);

            var results = await kernel.SendAsync(new SubmitCode("#!set --value x"));

            results.Events.Should().ContainSingle<CommandFailed>()
                   .Which.Message.Should().Be("Option '--name' is required.");
        }

        [Fact]
        public async Task value_option_is_required()
        {
            using var kernel = CreateKernel(Language.CSharp);

            var results = await kernel.SendAsync(new SubmitCode("#!set --name x"));

            results.Events.Should().ContainSingle<CommandFailed>()
                   .Which.Message.Should().Be("Option '--value' is required.");
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

        private CompositeKernel CreateCompositeKernel()
        {
            var composite = new CompositeKernel
            {
                CreateKernel(Language.CSharp),
                CreateKernel(Language.FSharp),
                CreateKernel(Language.PowerShell),
            };

            return composite;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}