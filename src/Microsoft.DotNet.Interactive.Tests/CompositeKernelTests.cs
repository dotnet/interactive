// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class CompositeKernelTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public CompositeKernelTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive()
        {
            var cSharpKernel = new CSharpKernel();
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                cSharpKernel,
                fSharpKernel,
            };
            kernel.DefaultKernelName = fSharpKernel.Name;

            using var events = kernel.KernelEvents.ToSubscribedList();

            var csharpCommand = new SubmitCode(@"
#!csharp
new [] {1,2,3}");
            await kernel.SendAsync(csharpCommand);
            
            var fsharpCommand = new SubmitCode(@"
#!fsharp
[1;2;3]");

            await kernel.SendAsync(fsharpCommand);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == csharpCommand);
            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == fsharpCommand);
        }

        [FactSkipLinux]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive_as_a_proxy_named_pipe()
        {
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                fSharpKernel
            }.UseProxyKernel();
            kernel.DefaultKernelName = fSharpKernel.Name;

            var pipeName = Guid.NewGuid().ToString();
            using var cSharpKernel = new CSharpKernel();
            Action doWait = () =>
                Task.Run(() => NamedPipeKernelServer.WaitForConnection(cSharpKernel, pipeName));
            doWait();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var proxyCommand = new SubmitCode($"#!connect test {pipeName}");

            await kernel.SendAsync(proxyCommand);

            var proxyCommand2 = new SubmitCode(@"
var x = 1 + 1;
x", targetKernelName: "test");

            await kernel.SendAsync(proxyCommand2);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == proxyCommand);
        }

        [FactSkipLinux]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive_as_a_proxy_named_pipe2()
        {
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                fSharpKernel
            }.UseProxyKernel();
            kernel.DefaultKernelName = fSharpKernel.Name;

            var pipeName = Guid.NewGuid().ToString();
            using var cSharpKernel = new CSharpKernel();
            Action doWait = () =>
                Task.Run(() => NamedPipeKernelServer.WaitForConnection(cSharpKernel, pipeName));
            doWait();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var proxyCommand = new SubmitCode($"#!connect test {pipeName}");

            await kernel.SendAsync(proxyCommand);

            var proxyCommand2 = new SubmitCode(@"
#!test
var x = 1 + 1;
x");

            await kernel.SendAsync(proxyCommand2);

            var proxyCommand3 = new SubmitCode(@"
#!test
var y = x + x;
y");

            await kernel.SendAsync(proxyCommand3);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == proxyCommand2);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == proxyCommand3);
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_using_kernel_alias_as_a_directive()
        {
            var cSharpKernel = new CSharpKernel();
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel();
            kernel.Add(cSharpKernel, new[] { "C#" });
            kernel.Add(fSharpKernel, new[] { "F#" });
            kernel.DefaultKernelName = fSharpKernel.Name;

            using var events = kernel.KernelEvents.ToSubscribedList();

            var csharpCommand = new SubmitCode(@"
#!C#
new [] {1,2,3}");
            await kernel.SendAsync(csharpCommand);
            
            var fsharpCommand = new SubmitCode(@"
#!F#
[1;2;3]");

            await kernel.SendAsync(fsharpCommand);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == csharpCommand);
            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == fsharpCommand);
        }

        [Theory(Timeout = 45000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task when_target_kernel_is_specified_and_not_found_then_command_fails(int kernelCount)
        {
            using var kernel = new CompositeKernel();
            using var events = kernel.KernelEvents.ToSubscribedList();
            foreach (var kernelName in Enumerable.Range(0, kernelCount).Select(i => $"kernel{i}"))
            {
                    kernel.Add(new FakeKernel(kernelName));
            }

            await kernel.SendAsync(
                new SubmitCode(
                    @"var x = 123;",
                    "unregistered kernel name"));

            events.Should()
                  .ContainSingle<CommandFailed>(cf => cf.Exception is NoSuitableKernelException);
        }

        [Fact]
        public void cannot_add_duplicated_named_kernels()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            kernel.Invoking(k => k.Add(new CSharpKernel()))
                .Should()
                .Throw<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be("Alias '#!csharp' is already in use.");
        }

        [Fact]
        public async Task can_handle_commands_targeting_composite_kernel_directly()
        {
            using var kernel = new CompositeKernel
            {
                new FakeKernel("fake")
                {
                    Handle = (command, context) => Task.CompletedTask
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();
            var submitCode = new SubmitCode("//command", kernel.Name)
            {
                Handler = (kernelCommand, context) => Task.CompletedTask
            };

            await kernel.SendAsync(submitCode);

            events.Should()
                .ContainSingle<CommandHandled>()
                .Which
                .Command
                .Should()
                .Be(submitCode);
        }

        [Fact]
        public async Task commands_targeting_compositeKernel_are_not_routed_to_childKernels()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();
            using var kernel = new CompositeKernel
            {
                new FakeKernel("fake")
                {
                    Handle = (kernelCommand, context) =>
                    {
                        receivedOnFakeKernel.Add(kernelCommand);
                        return Task.CompletedTask;
                    }
                }
            };

            var submitCode = new SubmitCode("//command", kernel.Name);

            await kernel.SendAsync(submitCode);
            receivedOnFakeKernel.Should()
                .BeEmpty();
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_by_setting_the_kernel_name_in_the_command()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();

            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = (kernelCommand, context) =>
                    {
                        receivedOnFakeKernel.Add(kernelCommand);
                        return Task.CompletedTask;
                    }
                }
            };

            await kernel.SendAsync(
                new SubmitCode(
                    @"var x = 123;",
                    "csharp"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!",
                    "fake"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"x",
                    "csharp"));

            receivedOnFakeKernel
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_as_a_default()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();

            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = (command, context) =>
                    {
                        receivedOnFakeKernel.Add(command);
                        return Task.CompletedTask;
                    }
                }
            };

            kernel.DefaultKernelName = "fake";

            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!"));

            receivedOnFakeKernel
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_as_a_default_via_an_alias()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();

            var fakeKernel = new FakeKernel("fake")
            {
                Handle = (command, context) =>
                {
                    receivedOnFakeKernel.Add(command);
                    return Task.CompletedTask;
                }
            };

            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            kernel.Add(fakeKernel, new[] { "totally-fake" });

            kernel.DefaultKernelName = "totally-fake";

            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!"));

            receivedOnFakeKernel
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task When_no_default_kernel_is_specified_then_kernel_directives_can_be_used()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FSharpKernel()
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#!csharp 
new [] {1,2,3}");
                
            events.Should().NotContainErrors();
        }

        [Fact]
        public void When_only_one_subkernel_is_present_then_default_kernel_name_returns_its_name()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            kernel.DefaultKernelName.Should().Be("csharp");
        }

        [Fact]
        public async Task Events_published_by_child_kernel_are_visible_in_parent_kernel()
        {
            var subKernel = new CSharpKernel();

            using var compositeKernel = new CompositeKernel
            {
                subKernel
            };

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await subKernel.SendAsync(new SubmitCode("var x = 1;"));

            events
                .Select(e => e.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CodeSubmissionReceived),
                    typeof(CompleteCodeSubmissionReceived),
                    typeof(CommandHandled));
        }

        [Fact]
        public async Task Deferred_commands_on_composite_kernel_are_execute_on_first_submission()
        {
            var deferredCommandExecuted = false;
            var subKernel = new CSharpKernel();

            using var compositeKernel = new CompositeKernel
            {
                subKernel
            };

            compositeKernel.DefaultKernelName = subKernel.Name;

            var deferred = new SubmitCode("placeholder")
            {
                Handler = (command, context) =>
                {
                    deferredCommandExecuted = true;
                    return Task.CompletedTask;
                }
            };
            
            compositeKernel.DeferCommand(deferred);

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("var x = 1;", targetKernelName: subKernel.Name));

            deferredCommandExecuted.Should().Be(true);

            events
                .Select(e => e.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CodeSubmissionReceived),
                    typeof(CompleteCodeSubmissionReceived),
                    typeof(CommandHandled));
        }

        [Fact]
        public async Task Deferred_commands_on_composite_kernel_can_use_directives()
        {
            var deferredCommandExecuted = false;
            var subKernel = new CSharpKernel();

            using var compositeKernel = new CompositeKernel
            {
                subKernel
            };
            var customDirective = new Command("#!customDirective")
            {
                Handler = CommandHandler.Create(() => { deferredCommandExecuted = true; })

            };
            compositeKernel.AddDirective(customDirective);

            compositeKernel.DefaultKernelName = subKernel.Name;

            var deferred = new SubmitCode("#!customDirective");

            compositeKernel.DeferCommand(deferred);

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("var x = 1;", targetKernelName: subKernel.Name));

            deferredCommandExecuted.Should().Be(true);

            events
                .Select(e => e.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CodeSubmissionReceived),
                    typeof(CompleteCodeSubmissionReceived),
                    typeof(CommandHandled));
        }

        [Fact]
        public void Child_kernels_are_disposed_when_CompositeKernel_is_disposed()
        {
            var csharpKernelWasDisposed = false;
            var fsharpKernelWasDisposed = false;

            var csharpKernel = new CSharpKernel();
            csharpKernel.RegisterForDisposal(() => csharpKernelWasDisposed = true);

            var fsharpKernel = new FSharpKernel();
            fsharpKernel.RegisterForDisposal(() => fsharpKernelWasDisposed = true);

            var compositeKernel = new CompositeKernel
            {
                csharpKernel,
                fsharpKernel
            };
            compositeKernel.Dispose();

            csharpKernelWasDisposed.Should().BeTrue();
            fsharpKernelWasDisposed.Should().BeTrue();
        }

        [Fact]
        public void When_frontend_environment_is_set_then_it_is_also_assigned_to_child_kernels()
        {
            using var compositeKernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            compositeKernel.FrontendEnvironment = new AutomationEnvironment();

            compositeKernel
                .ChildKernels
                .OfType<KernelBase>()
                .Single()
                .FrontendEnvironment
                .Should()
                .BeSameAs(compositeKernel.FrontendEnvironment);
        }
        
        [Fact]
        public void When_child_kernel_is_added_then_its_frontend_environment_is_obtained_from_the_parent()
        {
            using var compositeKernel = new CompositeKernel();

            compositeKernel.FrontendEnvironment = new AutomationEnvironment();

            compositeKernel.Add(new CSharpKernel());

            compositeKernel
                .ChildKernels
                .OfType<KernelBase>()
                .Single()
                .FrontendEnvironment
                .Should()
                .BeSameAs(compositeKernel.FrontendEnvironment);
        }
    }
}