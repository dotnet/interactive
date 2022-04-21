// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class VariableSharingWithinMagicCommandsTests
{
    [Fact]
    public async Task Magic_command_arguments_can_access_variables_from_the_current_kernel_via_interpolation()
    {
        using var kernel = CreateKernel();

        int receivedValue = 0;

        Option<int> valueOption = new("--value");
        Command shim = new("#!shim") { valueOption };
        shim.SetHandler((int value) =>
        {
            receivedValue = value;
        }, valueOption);

        kernel.FindKernel("csharp").AddDirective(shim);

        await kernel.SubmitCodeAsync("var x = 123;");

        var result = await kernel.SendAsync(new SubmitCode("#!shim --value @x", "csharp"));

        using var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();

        receivedValue.Should().Be(123);
    }

    private static CompositeKernel CreateKernel() =>
        new()
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new KeyValueStoreKernel()
        };
}