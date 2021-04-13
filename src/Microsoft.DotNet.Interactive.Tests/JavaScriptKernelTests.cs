// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class JavaScriptKernelTests
    {
        [Fact]
        public async Task javascript_kernel_emits_code_as_it_was_given()
        {
            using var kernel = new CompositeKernel
            {
                new JavaScriptKernel()
            };

            var scriptContent = "alert('Hello World!');";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode($"#!javascript\n{scriptContent}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .Select(v => v.Value)
                    .Cast<ScriptContent>()
                    .ToArray();

            formatted
                .Should()
                .ContainSingle()
                .Which
                .ScriptValue
                .Should()
                .Be($"\n{scriptContent}");
        }


        [Fact]
        public async Task javascript_kernel_forwards_commands_to_frontend()
        {
            var frontendEnvironment = new TestFrontendEnvironment();
            using var kernel = new CompositeKernel
            {
                new JavaScriptKernel
                {
                    FrontendEnvironment = frontendEnvironment
                }
            };
            
            kernel.FindKernel(JavaScriptKernel.DefaultKernelName).RegisterCommandType<CustomCommand>();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var command = new CustomCommand(JavaScriptKernel.DefaultKernelName);
            
            await kernel.SendAsync(command, CancellationToken.None);

            frontendEnvironment.ForwardedCommands.Should().Contain(command);
        }

        public class CustomCommand : KernelCommand
        {
            public CustomCommand(string targetKernelName) : base(targetKernelName: targetKernelName)
            {
                
            }
        }

        public class TestFrontendEnvironment : FrontendEnvironment
        {
            public List<KernelCommand> ForwardedCommands { get; } = new();
            public List<string> CodeSubmissions { get; } = new();

            public override Task ExecuteClientScript(string code, KernelInvocationContext context)
            {
                CodeSubmissions.Add(code);
                return Task.CompletedTask;
            }

            public override Task ForwardCommand(KernelCommand command, KernelInvocationContext context)
            {
                ForwardedCommands.Add(command);
                return Task.CompletedTask;
            }
        }
    }
}