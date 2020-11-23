// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class VariableSharingTests
    {
        [Theory]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            "(GetKernel(\"fsharp\") as Microsoft.DotNet.Interactive.DotNetKernel).TryGetVariable(\"x\", out int x);\nx")]
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

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{from}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"#!pwsh\n{codeToRead}");

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

        [Fact(Skip = "not implemented")]
        public async Task Directives_can_access_local_kernel_variables()
        {
            using var kernel = CreateKernel();
            kernel.DefaultKernelName = "csharp";
            var csharpKernel = (CSharpKernel) kernel.FindKernel("csharp");

            using var events = kernel.KernelEvents.ToSubscribedList();
            var receivedValue = 0;

            var directive = new Command("#!grab")
            {
                new Argument<int>("x")
            };
            directive.Handler = CommandHandler.Create<KernelInvocationContext, int>((context, x) =>
            {
                return receivedValue = x;
            });

            csharpKernel.AddDirective(directive);

            await kernel.SubmitCodeAsync("var x = 123;");
            await kernel.SubmitCodeAsync("#!grab $x");

            receivedValue.Should().Be(123);
        }

        private static CompositeKernel CreateKernel()
        {
            return new CompositeKernel
            {
                new CSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseDotNetVariableSharing(),
                new FSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseDefaultNamespaces() 
                    .UseDotNetVariableSharing(),
                new PowerShellKernel()
                    .UseDotNetVariableSharing()
            }.LogEventsToPocketLogger();
        }

        [Fact(Skip = "WIP")]
        public void Internal_types_are_shared_as_their_most_public_supertype()
        {
            throw new NotImplementedException("test not written");
        }
    }
}