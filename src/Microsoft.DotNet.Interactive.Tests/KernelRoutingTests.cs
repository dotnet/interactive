// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using KernelActionDirective = Microsoft.DotNet.Interactive.Directives.KernelActionDirective;

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
    public async Task When_target_kernel_name_is_specified_then_ProxyKernel_does_not_split_custom_magics()
    {
        using var localCompositeKernel = new CompositeKernel();
        using var remoteCompositeKernel = new CompositeKernel
        {
            new FakeKernel("csharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };

        var fakeMagic = new KernelActionDirective("#!fake");
        var fakeMagicWasCalled = false;
        remoteCompositeKernel.AddDirective(
            fakeMagic,
            (_, _) =>
            {
                fakeMagicWasCalled = true;
                return Task.CompletedTask;
            });

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
              .Host
              .ConnectProxyKernelOnDefaultConnectorAsync(
                  "csharp-proxy",
                  new(remoteCompositeKernel.Host.Uri, "csharp"));

        var code = """
            #!fake
            123
            """;

        var result = await localCompositeKernel.SendAsync(new SubmitCode(code, "csharp-proxy"));

        result.Events.Should().NotContainErrors();

        fakeMagicWasCalled.Should().BeTrue();

        result.Events.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should().Be(code);
    }

    [Fact]
    public async Task When_target_kernel_name_is_not_specified_then_ProxyKernel_does_not_split_custom_magics()
    {
        using var localCompositeKernel = new CompositeKernel();
        using var remoteCompositeKernel = new CompositeKernel
        {
            new FakeKernel("csharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };

        var fakeMagic = new KernelActionDirective("#!fake");
        var fakeMagicWasCalled = false;
        remoteCompositeKernel.AddDirective(
            fakeMagic,
            (_, _) =>
            {
                fakeMagicWasCalled = true;
                return Task.CompletedTask;
            });

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
              .Host
              .ConnectProxyKernelOnDefaultConnectorAsync(
                  "csharp-proxy",
                  new(remoteCompositeKernel.Host.Uri, "csharp"));

        var code = """
            #!fake
            123
            """;

        var result = await localCompositeKernel.SendAsync(new SubmitCode(code));

        result.Events.Should().NotContainErrors();

        fakeMagicWasCalled.Should().BeTrue();

        result.Events.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should().Be(code);
    }

    [Fact]
    public async Task When_target_kernel_name_is_specified_then_ProxyKernel_does_not_split_pound_r_and_pound_i()
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

        result.Events.Should().NotContainErrors();

        handledCommands.Should()
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .Be(code);
    }

    [Fact]
    public async Task When_target_kernel_name_is_not_specified_then_proxyKernel_does_not_split_pound_r_and_pound_i()
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
        await localCompositeKernel.SendAsync(command);

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
            new FakeKernel("csharp")
            {
                Handle = (_, _) => Task.CompletedTask
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };

        compositeKernel.DefaultKernelName = "fsharp";

        var command = new SubmitCode(@"Console.WriteLine(1);", targetKernelName: "csharp");

        await compositeKernel.SendAsync(command);

        command.RoutingSlip.ToUriArray().Should().BeEquivalentTo(
            new[]
            {
                "kernel://local/.NET?tag=arrived",
                "kernel://local/csharp?tag=arrived",
                "kernel://local/csharp",
                "kernel://local/.NET"
            });
    }

    [Fact]
    public async Task proxy_kernel_can_register_command_types_handled_by_remote()
    {
        using var localCompositeKernel = new CompositeKernel("vscode");
        using var remoteCompositeKernel = new CompositeKernel(".NET")
        {
            new FakeKernel("csharp")
            {
                Handle = (_, _) => Task.CompletedTask
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };
        RemoteCommand remoteCommandHandled = null;
        remoteCompositeKernel.FindKernelByName("csharp").RegisterCommandHandler<RemoteCommand>((command, context) =>
        {
            remoteCommandHandled = command;

            return Task.CompletedTask;
        });

        remoteCompositeKernel.DefaultKernelName = "fsharp";

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        await localCompositeKernel
            .Host
            .ConnectProxyKernelOnDefaultConnectorAsync(
                "csharp-proxy",
                new(remoteCompositeKernel.Host.Uri, "csharp"));

        localCompositeKernel.FindKernelByName("csharp-proxy").RegisterCommandType<RemoteCommand>();

        var command = new RemoteCommand("csharp-proxy");

        await localCompositeKernel.SendAsync(command);

        remoteCommandHandled.Should().NotBeNull();
    }

    public class RemoteCommand : KernelCommand
    {
        public RemoteCommand(string targetKernelName) : base(targetKernelName)
        {
        }
    }

    [Fact]
    public async Task commands_routing_slip_contains_proxy_kernels_that_have_been_traversed()
    {
        using var localCompositeKernel = new CompositeKernel("vscode");
        using var remoteCompositeKernel = new CompositeKernel(".NET")
        {
            new FakeKernel("csharp")
            {
                Handle = (_, _) => Task.CompletedTask
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
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

        command.RoutingSlip.ToUriArray().Should().BeEquivalentTo(
            new[]
            {
                "kernel://local/?tag=arrived",
                "kernel://local/csharp-proxy?tag=arrived",
                "kernel://remote/?tag=arrived",
                "kernel://remote/csharp?tag=arrived",
                "kernel://remote/csharp",
                "kernel://remote/",
                "kernel://local/csharp-proxy",
                "kernel://local/"
            });
    }

    [Fact]
    public async Task events_routing_slip_contains_kernels_that_have_been_traversed()
    {
        using var compositeKernel = new CompositeKernel
        {
            new FakeKernel("csharp")
            {
                Handle = (command, context) =>
                {
                    if (command is SubmitCode submitCode)
                    {
                        context.Publish(new ReturnValueProduced(submitCode.Code, command));
                    }

                    return Task.CompletedTask;
                }
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
        };

        compositeKernel.DefaultKernelName = "fsharp";

        var command = new SubmitCode(@"123", targetKernelName: "csharp");

        var result = await compositeKernel.SendAsync(command);

        result.Events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .RoutingSlip.ToUriArray().Should().ContainInOrder(
                  new[]
                  {
                      "kernel://local/csharp",
                      "kernel://local/.NET"
                  });
    }

    [Fact]
    public async Task events_routing_slip_contains_proxy_kernels_that_have_been_traversed()
    {
        using var localCompositeKernel = new CompositeKernel("vscode");
        using var remoteCompositeKernel = new CompositeKernel(".NET")
        {
            new FakeKernel("csharp")
            {
                Handle = (command, context) =>
                {
                    if (command is SubmitCode submitCode)
                    {
                        context.Publish(new ReturnValueProduced(submitCode.Code, command));
                    }

                    return Task.CompletedTask;
                }
            },
            new FakeKernel("fsharp")
            {
                Handle = (_, _) => Task.CompletedTask
            }
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

        result.Events
              .Should()
              .ContainSingle<ReturnValueProduced>()
              .Which
              .RoutingSlip.ToUriArray().Should().ContainInOrder(
                  new[]
                  {
                      "kernel://remote/csharp",
                      "kernel://remote/",
                      "kernel://local/csharp-proxy",
                      "kernel://local/"
                  });
    }
}