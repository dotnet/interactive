// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public class KernelRoutingTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public KernelRoutingTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public void RoutingSlip_includes_parent_RoutingSlip()
    {
        var parent = new RoutingSlip();
        parent.TryAdd(new Uri("kernel://a"));

        var child = new RoutingSlip(parent);
        child.TryAdd(new Uri("kernel://b"));

        child.Contains(parent).Should().BeTrue();
    }

    [Fact]
    public void RoutingSlip_identifies_childCommands()
    {
        var parent = new SubmitCode("code1");
        parent.RoutingSlip.TryAdd(new Uri("kernel://1"));
        parent.RoutingSlip.TryAdd(new Uri("kernel://2"));
        var child = new SubmitCode("code2");
        child.RoutingSlip.TryAdd(new Uri("kernel://1"));
        child.RoutingSlip.TryAdd(new Uri("kernel://2"));
        child.RoutingSlip.TryAdd(new Uri("kernel://5"));

        child.IsChildCommand(parent).Should().BeTrue();
    }

    [Fact]
    public async Task When_target_kernel_name_is_specified_then_ProxyKernel_does_not_split_magics()
    {
        var handledCommands = new List<KernelCommand>();
        using var localCompositeKernel = new CompositeKernel();
        using var remoteCompositeKernel = new CompositeKernel
        {
            new FakeKernel("csharp")
            {
                Handle = (command, _) =>
                {
                    handledCommands.Add(command);
                    return Task.CompletedTask;
                }
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
            .Host
            .ConnectProxyKernelOnDefaultConnectorAsync(
                "csharp-proxy",
                new(remoteCompositeKernel.Host.Uri, "csharp"));

        var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

        var command = new SubmitCode(code, "csharp-proxy");
        var result = await localCompositeKernel.SendAsync(command);

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();

        handledCommands.Should()
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .Be(code);
    }

    [Fact]
    public async Task When_target_kernel_name_is_not_specified_then_proxyKernel_does_not_split_magics()
    {
        var handledCommands = new List<KernelCommand>();
        using var localCompositeKernel = new CompositeKernel();
        using var remoteCompositeKernel = new CompositeKernel
        {
            new FakeKernel("csharp")
            {
                Handle = (command, _) =>
                {
                    handledCommands.Add(command);
                    return Task.CompletedTask;
                }
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
            .Host
            .ConnectProxyKernelOnDefaultConnectorAsync(
                "csharp-proxy",
                new(remoteCompositeKernel.Host.Uri, "csharp"));

        var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

        var command = new SubmitCode(code);
        var result = await localCompositeKernel.SendAsync(command);
        var events = result.KernelEvents.ToSubscribedList();

        handledCommands.Should()
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .Be(code);
    }

    [Fact]
    public async Task A_default_kernel_name_can_be_specified_to_handle_a_command_type()
    {
        KernelCommand receivedByKernelOne = null;
        KernelCommand receivedByKernelTwo = null;

        var kernelOne = new FakeKernel("one");

        kernelOne.AddMiddleware((command, context, next) =>
        {
            receivedByKernelOne = command;
            return Task.CompletedTask;
        });

        var kernelTwo = new FakeKernel("two");

        kernelTwo.AddMiddleware((command, context, next) =>
        {
            receivedByKernelTwo = command;
            return Task.CompletedTask;
        });

        using var compositeKernel = new CompositeKernel
        {
            kernelOne,
            kernelTwo
        };

        compositeKernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), "one");

        var command = new RequestInput("Input please!");

        await compositeKernel.SendAsync(command);

        receivedByKernelOne.Should().Be(command);
    }

    [Fact]
    public async Task commands_routing_slip_contains_kernels_that_have_been_traversed()
    {
        using var compositeKernel = new CompositeKernel
        {
            new CSharpKernel(),
            new FSharpKernel()
        };

        compositeKernel.DefaultKernelName = "fsharp";

        var command = new SubmitCode(@"Console.WriteLine(1);", targetKernelName: "csharp");

        await compositeKernel.SendAsync(command);

        command.RoutingSlip.Should().BeEquivalentTo(
            new[]
            {
                new Uri("kernel://local/.NET", UriKind.Absolute), 
                new Uri("kernel://local/csharp", UriKind.Absolute)
            });
    }

    [Fact]
    public async Task commands_routing_slip_contains_the_uris_of_parent_command()
    {
        using var compositeKernel = new CompositeKernel
        {
            new CSharpKernel(),
            new FSharpKernel()
        };

        compositeKernel.DefaultKernelName = "fsharp";

        var command = new SubmitCode(@"
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
var command = new SubmitCode(@""1+1"", targetKernelName: ""fsharp"");
await Kernel.Root.SendAsync(command);", targetKernelName: "csharp");

        var result = await compositeKernel.SendAsync(command);

        var events = result.KernelEvents.ToSubscribedList();

        var fsharpEvent = events.OfType<ReturnValueProduced>().First();

        fsharpEvent.Command.RoutingSlip.Should().BeEquivalentTo(
            new[]
            {
                new Uri("kernel://local/.NET", UriKind.Absolute),
                new Uri("kernel://local/csharp", UriKind.Absolute),
                new Uri("kernel://local/fsharp", UriKind.Absolute)
            });
    }

    [Fact]
    public async Task commands_routing_slip_contains_proxy_kernels_that_have_been_traversed()
    {
        using var localCompositeKernel = new CompositeKernel("vscode");
        using var remoteCompositeKernel = new CompositeKernel(".NET")
        {
            new CSharpKernel(),
            new FSharpKernel()
        };
        
        remoteCompositeKernel.DefaultKernelName = "fsharp";
        
        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
            .Host
            .ConnectProxyKernelOnDefaultConnectorAsync(
                "csharp-proxy",
                new(remoteCompositeKernel.Host.Uri, "csharp"));
        
        var command = new SubmitCode(@"Console.WriteLine(1);", targetKernelName: "csharp-proxy");

        await localCompositeKernel.SendAsync(command);

        command.RoutingSlip.Should().BeEquivalentTo(
            new[]
            {
                new Uri("kernel://local/", UriKind.Absolute),
                new Uri("kernel://local/csharp-proxy", UriKind.Absolute),
                new Uri("kernel://remote/", UriKind.Absolute),
                new Uri("kernel://remote/csharp", UriKind.Absolute)
            });
    }

    [Fact]
    public async Task events_routing_slip_contains_kernels_that_have_been_traversed()
    {
        using var compositeKernel = new CompositeKernel
        {
            new CSharpKernel(),
            new FSharpKernel()
        };

        compositeKernel.DefaultKernelName = "fsharp";

        var command = new SubmitCode(@"Console.WriteLine(1);", targetKernelName: "csharp");

        var result = await compositeKernel.SendAsync(command);

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<StandardOutputValueProduced>()
            .Which
            .RoutingSlip.Should().ContainInOrder(
            new[]
            {
                new Uri("kernel://local/csharp", UriKind.Absolute),
                new Uri("kernel://local/.NET", UriKind.Absolute)
               
            });
    }

    [Fact]
    public async Task events_routing_slip_contains_proxy_kernels_that_have_been_traversed()
    {
        using var localCompositeKernel = new CompositeKernel("vscode");
        using var remoteCompositeKernel = new CompositeKernel(".NET")
        {
            new CSharpKernel(),
            new FSharpKernel()
        };

        remoteCompositeKernel.DefaultKernelName = "fsharp";

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
            .Host
            .ConnectProxyKernelOnDefaultConnectorAsync(
                "csharp-proxy",
                new(remoteCompositeKernel.Host.Uri, "csharp"));

        var command = new SubmitCode("123", targetKernelName: "csharp-proxy");

        var result = await localCompositeKernel.SendAsync(command);

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<ReturnValueProduced>()
            .Which
            .RoutingSlip.Should().ContainInOrder(
            new[]
            {
                new Uri("kernel://remote/csharp", UriKind.Absolute),
                new Uri("kernel://remote/", UriKind.Absolute),
                new Uri("kernel://local/csharp-proxy", UriKind.Absolute),
                new Uri("kernel://local/", UriKind.Absolute)
            });
    }
}