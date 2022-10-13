﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelCommandNestingTests : LanguageKernelTestBase
    {
        public KernelCommandNestingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_do_not_publish_CommandSucceeded_to_the_outer_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel ( "cs1" ),
                new CSharpKernel ( "cs2" )
            };
            var kernelEvents = kernel.KernelEvents.ToSubscribedList();
            var command = new SubmitCode(@$"
#!cs1
using {typeof(Kernel).Namespace};
using {typeof(KernelCommand).Namespace};
await Kernel.Root.SendAsync(new SubmitCode(""1+1"", ""cs2""));
");
            await kernel.SendAsync(command);

            using var _ = new AssertionScope();
            kernelEvents.Should()
                .ContainSingle<CommandSucceeded>(e => e.Command == command);

            kernelEvents.Should()
                .NotContain(e =>
                                e is CommandSucceeded &&
                                e.Command.TargetKernelName == "cs2");
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_do_not_publish_CommandFailed_to_the_outer_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel ( "cs1" ),
                new CSharpKernel ( "cs2" )
            };
            var kernelEvents = kernel.KernelEvents.ToSubscribedList();
            var command = new SubmitCode($@"
#!cs1
using {typeof(Kernel).Namespace};
using {typeof(KernelCommand).Namespace};
await Kernel.Root.SendAsync(new SubmitCode(""error"", ""cs2""));
");
            await kernel.SendAsync(command);

            kernelEvents.Should()
                .ContainSingle<CommandSucceeded>(e => e.Command == command);

            kernelEvents
                .Should()
                .NotContain(e => e is CommandFailed);
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_publish_error_events_for_failures()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel ( "cs1" ),
                new CSharpKernel ( "cs2" )
            };

            var command = new SubmitCode($@"
#!cs1
using {typeof(Kernel).Namespace};
using {typeof(KernelCommand).Namespace};
await Kernel.Root.SendAsync(new SubmitCode(""error"", ""cs2""));
");
            var result = await kernel.SendAsync(command);

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<ErrorProduced>()
                  .Which
                  .Message
                  .Should()
                  .Be("(1,1): error CS0103: The name 'error' does not exist in the current context");
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

await result.KernelEvents.LastAsync()
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events
                .Should()
                .ContainSingle<ReturnValueProduced>(e => e.Value is CommandSucceeded);
        }

        [Fact]
        public async Task Commands_sent_within_the_code_of_another_command_publish_CommandFailed_to_the_inner_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel ( "cs1" ),
                new CSharpKernel ( "cs2" )
            };

            var result = await kernel.SendAsync(new SubmitCode(
                                                    @"
using System.Reactive.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

var result = await Kernel.Root.SendAsync(new SubmitCode(""nope"", ""cs2""));

await result.KernelEvents.LastAsync()
", "cs1"));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<ReturnValueProduced>(e => e.Value is CommandFailed);
        }
    }
}