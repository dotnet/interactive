// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelBaseTests
    {
        private ITestOutputHelper _output;

        public KernelBaseTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Queued_initialization_command_is_not_executed_prior_to_first_submission()
        {
            var receivedCommands = new List<IKernelCommand>();

            var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            kernel.DeferCommand(new SubmitCode("hello"));
            kernel.DeferCommand(new SubmitCode("world!"));

            receivedCommands.Should().BeEmpty();
        }

        [Fact]
        public async Task Queued_initialization_command_is_executed_on_to_first_submission()
        {
            var receivedCommands = new List<IKernelCommand>();

            var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            kernel.DeferCommand(new SubmitCode("one"));
            kernel.DeferCommand(new SubmitCode("two"));

            await kernel.SendAsync(new SubmitCode("three"));

            var x = receivedCommands
                    .Select(c => c is SubmitCode submitCode ? submitCode.Code : c.ToString())
                    .Should()
                    .BeEquivalentSequenceTo("one", "two", "three");
        }

        [Fact]
        public async Task Middleware_is_only_executed_once_per_command()
        {
            var middeware1Count = 0;
            var middeware2Count = 0;
            var middeware3Count = 0;

          using  var kernel = new FakeKernel();

            kernel.AddMiddleware(async (command, context, next) => {
                middeware1Count++;
                await next(command, context);
            },"one");
            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware2Count++;
                await next(command, context);
            },"two");
            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware3Count++;
                await next(command, context);
            }, "three");

            await kernel.SendAsync(new SubmitCode("123"));

            middeware1Count.Should().Be(1);
            middeware2Count.Should().Be(1);
            middeware3Count.Should().Be(1);
        }
    }
}