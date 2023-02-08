// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Connection;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public partial class KernelTests
{
    public class SubmissionCount
    {
        [Fact]
        public async Task Split_submissions_only_increment_count_by_one()
        {
            using var kernel = new CompositeKernel
            {
                new FakeKernel("one"),
                new FakeKernel("two"),
            };

            await kernel.SubmitCodeAsync(@"
#!one
// do something
#!two
// do something
");

            kernel.SubmissionCount.Should().Be(1);
        }

        [Fact]
        public async Task SubmitCode_is_the_only_command_that_increments_submission_count()
        {
            using var kernel = new CompositeKernel
            {
                new FakeKernel("one"),
                new FakeKernel("two"),
            };

            foreach (var command in SerializationTests.Commands().SelectMany(cs => cs).Where(c => c is not SubmitCode))
            {
                await kernel.SendAsync((KernelCommand)command);
            }

           
            kernel.SubmissionCount.Should().Be(0);
        }

        [Fact]
        public async Task Deferred_commands_do_not_increment_count()
        {
            using var kernel = new CompositeKernel
            {
                new FakeKernel("one"),
                new FakeKernel("two"),
            };

            kernel.DeferCommand(new SubmitCode(""));

            await kernel.SendAsync(new RequestKernelInfo());

            kernel.SubmissionCount.Should().Be(0);
        }

        [Fact]
        public async Task Nested_submissions_do_not_increment_submission_count()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
            };

            var result = await kernel.SendAsync(new SubmitCode(@"
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

await Kernel.Root.SendAsync(new SubmitCode(""123"", ""fake""));
await Kernel.Root.SendAsync(new SubmitCode(""456"", ""fake""));
", "csharp"));

            result.Events.Should().NotContainErrors();

            kernel.SubmissionCount.Should().Be(1);
        }

        [Fact]
        public async Task Non_user_submissions_do_not_increment_submission_count()
        {
            var csharp2 = new CSharpKernel("csharp2");

            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                csharp2
            };

            using var _ = kernel
                          .KernelEvents
                          .OfType<DisplayEvent>()
                          .Subscribe(@event =>
                          {
                              if (@event.Command.TargetKernelName == "csharp2")
                              {
                                  return;
                              }

                              var command = new SubmitCode($"\"Received {@event.Value}\"");

                              csharp2.SendAsync(command).ConfigureAwait(false);
                          });

            await kernel.SendAsync(new SubmitCode("1", "csharp"));
            await kernel.SendAsync(new SubmitCode("2", "csharp"));

            kernel.SubmissionCount.Should().Be(2);
        }

        [Fact]
        public async Task Failed_commands_increment_submission_count()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            await kernel.SubmitCodeAsync("this does not compile");

            kernel.SubmissionCount.Should().Be(1);
        }
    }
}