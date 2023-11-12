// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public partial class KernelCommandNestingTests
{
    public class KernelCommandResult_Events
    {
        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_do_not_publish_CommandSucceeded_to_the_outer_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel("cs1"),
                new CSharpKernel("cs2")
            };
            var command = new SubmitCode(@$"
#!cs1
using {typeof(Kernel).Namespace};
using {typeof(KernelCommand).Namespace};
await Kernel.Root.SendAsync(new SubmitCode(""1+1"", ""cs2""));
");
            var result = await kernel.SendAsync(command);

            using var _ = new AssertionScope();

            result.Events.Should().ContainSingle<CommandSucceeded>(e => e.Command == command);

            result.Events.Should()
                  .NotContain(e =>
                                  e is CommandSucceeded &&
                                  e.Command.TargetKernelName == "cs2");
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_do_not_publish_CommandFailed_to_the_outer_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel("cs1"),
                new CSharpKernel("cs2")
            };
            var command = new SubmitCode($@"
#!cs1
using {typeof(Kernel).Namespace};
using {typeof(KernelCommand).Namespace};
await Kernel.Root.SendAsync(new SubmitCode(""error"", ""cs2""));
");
            var result = await kernel.SendAsync(command);

            result.Events.Should()
                  .ContainSingle<CommandSucceeded>(e => e.Command == command);

            result.Events
                  .Should()
                  .NotContain(e => e is CommandFailed);
        }

        [Fact] 
        public async Task Commands_sent_within_the_code_of_another_command_do_not_publish_events_to_the_outer_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel("cs1"),
                new CSharpKernel("cs2")
            };

            var command = new SubmitCode($"""
                using {typeof(Kernel).Namespace};
                using {typeof(KernelCommand).Namespace};
                var result = await Kernel.Root.SendAsync(new SubmitCode("123.Display();\n456", "cs2"));
                """, "cs1");

            var result = await kernel.SendAsync(command);

            using var _ = new AssertionScope();
            result.Events.Should().NotContainErrors();
            result.Events.Should().NotContain(e => e is DisplayedValueProduced);
            result.Events.Should().NotContain(e => e is ReturnValueProduced);
        }

        [Fact]
        public async Task Commands_sent_via_API_within_a_split_submission_do_not_bubble_events()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel("cs1"),
                new CSharpKernel("cs2")
            };

            var command = new SubmitCode($"""
                #!cs1
                using {typeof(Kernel).Namespace};
                using {typeof(KernelCommand).Namespace};
                var result = await Kernel.Root.SendAsync(new SubmitCode("123.Display();\n456", "cs2"));

                #!cs2
                Console.WriteLine(789);
                """);

            var result = await kernel.SendAsync(command);

            using var _ = new AssertionScope();
            result.Events.Should().NotContainErrors();
            result.Events.Should().NotContain(e => e is DisplayedValueProduced);
            result.Events.Should().NotContain(e => e is ReturnValueProduced);
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_publish_CommandSucceeded_to_the_inner_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FSharpKernel()
            };
            kernel.DefaultKernelName = "csharp";

            var result = await kernel.SubmitCodeAsync(
                             @"
using System.Reactive.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

var result = await Kernel.Root.SendAsync(new SubmitCode(""123"", ""fsharp""));

result.Events.Last()
");

            result.Events.Should().NotContainErrors();

            result.Events
                  .Should()
                  .ContainSingle<ReturnValueProduced>(e => e.Value is CommandSucceeded);
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_publish_CommandFailed_to_the_inner_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel("cs1"),
                new CSharpKernel("cs2")
            };

            var result = await kernel.SendAsync(new SubmitCode(
                                                    @"
using System.Reactive.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

var result = await Kernel.Root.SendAsync(new SubmitCode(""nope"", ""cs2""));

result.Events.Last()
", "cs1"));

            result.Events
                  .Should()
                  .ContainSingle<ReturnValueProduced>(e => e.Value is CommandFailed);
        }
    }
}