// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
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
        public void Deferred_initialization_command_is_not_executed_prior_to_first_submission()
        {
            var receivedCommands = new List<IKernelCommand>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            kernel.DeferCommand(new SubmitCode("hello"));
            kernel.DeferCommand(new SubmitCode("world!"));

            receivedCommands.Should().BeEmpty();
        }

        [Fact]
        public async Task Deferred_initialization_command_is_executed_on_first_submission()
        {
            var receivedCommands = new List<IKernelCommand>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            kernel.DeferCommand(new SubmitCode("one"));
            kernel.DeferCommand(new SubmitCode("two"));

            await kernel.SendAsync(new SubmitCode("three"));

            receivedCommands
                .Select(c => c is SubmitCode submitCode ? submitCode.Code : c.ToString())
                .Should()
                .BeEquivalentSequenceTo("one", "two", "three");
        }

        [Fact]
        public async Task Split_pound_r_and_i_commands_are_executed_before_other_cell_contents()
        {
            var receivedCommands = new List<IKernelCommand>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };


            var path = Path.GetTempFileName();

            kernel.DeferCommand(new SubmitCode($@"
#r ""{path}""
using Some.Namespace; 
"));


            await kernel.SubmitCodeAsync("// the code");





            throw new NotImplementedException("test not written");
        }

        [Fact]
        public void Split_non_referencing_directives_are_executed_in_textual_order()
        {
            var receivedCommands = new List<IKernelCommand>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            throw new NotImplementedException("test not written");
        }

        [Fact]
        public async Task Middleware_is_only_executed_once_per_command()
        {
            var middeware1Count = 0;
            var middeware2Count = 0;
            var middeware3Count = 0;

            using var kernel = new FakeKernel();

            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware1Count++;
                await next(command, context);
            }, "one");
            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware2Count++;
                await next(command, context);
            }, "two");
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