// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class SetMagicCommandTests
{
    [Theory]
    [InlineData(Language.CSharp)]
    public async Task can_set_value_using_return_value(Language language)
    {
        using var kernel = CreateKernel(language).UseSet();

        await kernel.SendAsync(new SubmitCode(@"
#!set --name x --from-result
1+3"));
        var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo(4);

    }

    [Theory]
    [InlineData(Language.CSharp)]
    public async Task set_value_using_return_value_fails_when_there_is_no_ReturnValueProduced(Language language)
    {
        using var kernel = CreateKernel(language).UseSet();

        var results  = await kernel.SendAsync(new SubmitCode(@"
#!set --name x --from-result
var num = 1+3;"));

        var events = results.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<CommandFailed>()
            .Which.Message.Should().Be("The command was expected to produce a ReturnValueProduced event.");

    }

    private static Kernel CreateKernel(Language language)
    {
        return language switch
        {
            Language.CSharp =>
                new CSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseValueSharing(),
            Language.FSharp =>
                new FSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseValueSharing(),
            Language.PowerShell =>
                new PowerShellKernel()
                    .UseValueSharing(),
        };
    }
}