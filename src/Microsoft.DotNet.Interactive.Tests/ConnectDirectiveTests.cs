// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class ConnectDirectiveTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public ConnectDirectiveTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [Fact]
    public void connect_command_is_not_available_by_default()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.Directives
            .Should()
            .NotContain(c => c.Name == "#!connect");
    }

    [Fact]
    public void connect_command_is_available_when_a_user_adds_a_kernel_connection_type()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.AddKernelConnector(new ConnectNamedPipeCommand());

        compositeKernel.Directives
            .Should()
            .Contain(c => c.Name == "#!connect");
    }

    [Fact]
    public async Task When_a_kernel_is_connected_then_information_about_it_is_displayed()
    {
        using var kernel = CreateKernelWithConnectableFakeKernel(new FakeKernel("my-fake-kernel"));

        var result = await kernel.SubmitCodeAsync("#!connect fake --kernel-name my-fake-kernel --fakeness-level 9000");

        result.Events
              .Should()
              .NotContainErrors()
              .And
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle()
              .Which
              .Value
              .Should()
              .Be("Kernel added: #!my-fake-kernel");
    }

    [Fact]
    public async Task When_a_new_kernel_is_connected_then_it_becomes_addressable_by_name()
    {
        var wasCalled = false;
        var fakeKernel = new FakeKernel("my-fake-kernel")
        {
            Handle = (_, _) =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            }
        };

        using var kernel = CreateKernelWithConnectableFakeKernel(fakeKernel);

        await kernel.SubmitCodeAsync("#!connect fake --kernel-name my-fake-kernel --fakeness-level 9000");

        await kernel.SubmitCodeAsync(@"
#!my-fake-kernel
hello!
");
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Connected_kernels_are_disposed_when_composite_kernel_is_disposed()
    {
        var disposed = false;

        var fakeKernel = new FakeKernel();
        fakeKernel.RegisterForDisposal(() => disposed = true);

        using var compositeKernel = CreateKernelWithConnectableFakeKernel(fakeKernel);

        var result = await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name my-fake-kernel --fakeness-level 9000");

        result.Events.Should().NotContainErrors();

        compositeKernel.Dispose();

        disposed.Should().BeTrue();
    }

    [Fact]
    public async Task Connected_kernel_help_description_provides_context_about_the_connection()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.AddKernelConnector(
            new ConnectFakeKernelCommand("fake", "Connects the fake kernel", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name fake-kernel");

        var result = await compositeKernel.SubmitCodeAsync("#!fake-kernel -h");

        result.Events.Should()
              .ContainSingle<StandardOutputValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle()
              .Which
              .Value
              .Should()
              .ContainAll("#!fake-kernel", "Connects the fake kernel (Connected kernel)");
    }

    [Fact]
    public async Task Multiple_connections_can_be_created_using_the_same_connection_type()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.AddKernelConnector(
            new ConnectFakeKernelCommand("fake", "Connects the fake kernel", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name fake1");
        await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name fake2");

        compositeKernel
            .Should()
            .ContainSingle(k => k.Name == "fake1")
            .And
            .ContainSingle(k => k.Name == "fake2");
    }

    private static Kernel CreateKernelWithConnectableFakeKernel(FakeKernel fakeKernel)
    {
        var compositeKernel = new CompositeKernel();

        compositeKernel.AddKernelConnector(
            new ConnectFakeKernelCommand("fake", "Connects the fake kernel", _ => Task.FromResult<Kernel>(fakeKernel)));

        return compositeKernel;
    }

    public class ConnectFakeKernelCommand : ConnectKernelCommand
    {
        private readonly Func<string, Task<Kernel>> _createKernel;

        public ConnectFakeKernelCommand(
            string name,
            string description,
            Func<string, Task<Kernel>> createKernel) : base(name, description)

        {
            AddOption(FakenessLevelOption);

            ConnectedKernelDescription = description;

            _createKernel = createKernel;
        }

        public Option<int> FakenessLevelOption { get; } =
            new("--fakeness-level");

        public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
            KernelInvocationContext context,
            InvocationContext commandLineContext)
        {
            var connector = new FakeKernelConnector();
            connector.CreateKernel = _createKernel;
            var kernel = await connector.CreateKernelAsync(commandLineContext.ParseResult.GetValueForOption(KernelNameOption));
            return new[] { kernel };
        }
    }
}

public class FakeKernelConnector 
{
    public int FakenessLevel { get; set; }

    public Func<string, Task<Kernel>> CreateKernel { get; set; }

    public Task<Kernel> CreateKernelAsync(string kernelName) => CreateKernel(kernelName);
}