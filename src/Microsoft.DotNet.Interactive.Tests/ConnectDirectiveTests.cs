﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;
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

        compositeKernel
            .KernelInfo
            .SupportedDirectives
            .Should()
            .NotContain(c => c.Name == "#!connect");
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
    public async Task Multiple_connections_can_be_created_using_the_same_connection_type()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.AddConnectDirective(
            new ConnectFakeKernelDirective("fake", "Connects the fake kernel", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name fake1");
        await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name fake2");

        compositeKernel
            .Should()
            .ContainSingle(k => k.Name == "fake1")
            .And
            .ContainSingle(k => k.Name == "fake2");
    }

    [Fact] // https://github.com/dotnet/interactive/issues/3711
    public async Task When_a_duplicate_name_is_used_then_a_friendly_error_is_shown()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.AddConnectDirective(
            new ConnectFakeKernelDirective("fake", "Connects the fake kernel", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        var result = await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name myFake");
        result.Events.Should().NotContainErrors();

        result = await compositeKernel.SubmitCodeAsync("#!connect fake --kernel-name myFake");
        result.Events
              .Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Be("(1,1): error DNI211: The kernel name or alias 'myFake' is already in use.");
    }

    private static Kernel CreateKernelWithConnectableFakeKernel(FakeKernel fakeKernel)
    {
        var compositeKernel = new CompositeKernel();

        compositeKernel.AddConnectDirective(
            new ConnectFakeKernelDirective("fake", "Connects the fake kernel", _ => Task.FromResult<Kernel>(fakeKernel)));

        return compositeKernel;
    }

    public class ConnectFakeKernelDirective : ConnectKernelDirective<ConnectFakeKernel>
    {
        private readonly Func<string, Task<Kernel>> _createKernel;

        public ConnectFakeKernelDirective(
            string name,
            string description,
            Func<string, Task<Kernel>> createKernel) : base(name, description)
        {
            KernelCommandType = typeof(ConnectFakeKernel);
            Parameters.Add(FakenessLevelParameter);
            ConnectedKernelDescription = description;

            _createKernel = createKernel;
        }

        public KernelDirectiveParameter FakenessLevelParameter { get; } =
            new("--fakeness-level");

        public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
            ConnectFakeKernel command,
            KernelInvocationContext context)
        {
            var kernel = await _createKernel(command.ConnectedKernelName);

            return new[] { kernel };
        }
    }

    public class ConnectFakeKernel : ConnectKernelCommand
    {
        public ConnectFakeKernel(string connectedKernelName) : base(connectedKernelName)
        {
        }
    }
}
