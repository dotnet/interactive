// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class VariableSharingWithinMagicCommandsTests : IDisposable
{
    private readonly CompositeKernel _kernel;
    private string receivedValue = null;

    public VariableSharingWithinMagicCommandsTests()
    {
        _kernel = CreateKernel();

        KernelActionDirective shim = new("#!shim")
        {
            KernelCommandType = typeof(ShimCommand),
            Parameters =
            {
                new KernelDirectiveParameter("--value")
            }
        };

        _kernel.FindKernelByName("csharp").AddDirective<ShimCommand>(shim, (command, context) =>
        {
            receivedValue = command.Value;
            return Task.CompletedTask;
        });
    }

    public class ShimCommand : KernelCommand
    {
        public string Value { get; set; }
    }

    public void Dispose()
    {
        _kernel.Dispose();
    }

    [Fact]
    public async Task Magic_commands_can_interpolate_variables_from_the_current_kernel()
    {
        await _kernel.SendAsync(new SubmitCode("var x = 123;", "csharp"));

        var result = await _kernel.SendAsync(new SubmitCode("#!shim --value @x", "csharp"));

        result.Events.Should().NotContainErrors();

        receivedValue.Should().Be("123");
    }

    [Fact]
    public async Task Magic_commands_can_interpolate_variables_from_a_different_kernel()
    {
        var valueX = "value from the C# kernel";

        await _kernel.SubmitCodeAsync($"""var x = "{valueX}"; """);

        var result = await _kernel.SendAsync(new SubmitCode("#!shim --value @csharp:x", "csharp"));

        result.Events.Should().NotContainErrors();

        receivedValue.Should().Be(valueX);
    }

    [Fact]
    public async Task When_variable_does_not_exist_then_an_error_is_returned()
    {
        var result = await _kernel.SendAsync(new SubmitCode("#!shim --value @x", "csharp"));

        receivedValue.Should().BeNull();

        result.Events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Contain("Value 'x' not found in kernel csharp");
    }

    [Fact]
    public async Task Language_service_requests_do_not_trigger_value_interpolation()
    {
        var inputWasRequested = false;
        _kernel.RegisterCommandHandler<RequestInput>((input, context) =>
        {
            inputWasRequested = true;
            return Task.CompletedTask;
        });
        _kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), _kernel.Name);

        var code = "#!share @input:stuff";
        var result = await _kernel.SendAsync(new RequestCompletions(code, new LinePosition(0, code.Length)));

        result.Events.Should().NotContainErrors();

        inputWasRequested.Should().BeFalse();
    }

    private static CompositeKernel CreateKernel()
    {
        var kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new KeyValueStoreKernel()
        };
        kernel.DefaultKernelName = "csharp";
        return kernel;
    }
}