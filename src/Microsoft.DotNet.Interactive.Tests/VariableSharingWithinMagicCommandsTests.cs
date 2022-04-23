// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class VariableSharingWithinMagicCommandsTests
{
    [Fact]
    public async Task Magic_commands_can_interpolate_variables_from_the_current_kernel()
    {
        using var kernel = CreateKernel();

        int receivedValue = 0;

        Option<int> valueOption = new("--value");
        Command shim = new("#!shim") { valueOption };
        shim.SetHandler((int value) => receivedValue = value, valueOption);

        kernel.FindKernel("csharp").AddDirective(shim);

        await kernel.SendAsync(new SubmitCode("var x = 123;", "csharp"));

        var result = await kernel.SendAsync(new SubmitCode("#!shim --value @x", "csharp"));

        using var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();

        receivedValue.Should().Be(123);
    }

    [Fact]
    public async Task Magic_commands_can_interpolate_variables_from_a_different_kernel()
    {
        using var kernel = CreateKernel();

        string receivedValue = null;

        Option<string> valueOption = new("--value");
        Command shim = new("#!shim") { valueOption };
        shim.SetHandler((string value) => receivedValue = value, valueOption);

        kernel.FindKernel("csharp").AddDirective(shim);

        var valueX = "value from the value kernel";

        await kernel.SendAsync(new SubmitCode($"#!value-kernel --name x\n{valueX}"));

        var result = await kernel.SendAsync(new SubmitCode("#!shim --value @value-kernel:x", "csharp"));

        using var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();

        receivedValue.Should().Be(valueX);
    }

    [Fact]
    public async Task Magic_commands_cannot_interpolate_complex_objects()
    {
        using var kernel = CreateKernel();

        int receivedValue = 0;

        Option<int> valueOption = new("--value");
        Command shim = new("#!shim") { valueOption };
        shim.SetHandler((int value) => receivedValue = value, valueOption);

        kernel.FindKernel("csharp").AddDirective(shim);

        await kernel.SendAsync(new SubmitCode("var x = new { name = \"my object\", shareability = 0 };", "csharp"));

        var result = await kernel.SendAsync(new SubmitCode("#!shim --value @x", "csharp"));

        using var events = result.KernelEvents.ToSubscribedList();

        events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Contain("Value @x cannot be interpolated into magic command:\n{\"name\":\"my object\",\"shareability\":0}");
    }

    [Fact]
    public async Task When_variable_does_not_exist_then_an_error_is_returned()
    {
         using var kernel = CreateKernel();

        string receivedValue = null;

        Option<string> valueOption = new("--value");
        Command shim = new("#!shim") { valueOption };
        shim.SetHandler((string value) => receivedValue = value, valueOption);

        kernel.FindKernel("csharp").AddDirective(shim);

        var result = await kernel.SendAsync(new SubmitCode("#!shim --value @x", "csharp"));

        using var events = result.KernelEvents.ToSubscribedList();

        receivedValue.Should().BeNull();

        events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Contain("Cannot find value named: x");
    }

    private static CompositeKernel CreateKernel() =>
        new()
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new KeyValueStoreKernel("value-kernel")
        };
}