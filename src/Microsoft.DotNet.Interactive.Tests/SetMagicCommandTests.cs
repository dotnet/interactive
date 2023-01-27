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
    [Fact]
    public async Task can_set_value_using_return_value()
    {
        using var kernel = CreateKernel(Language.CSharp).UseSet();

        await kernel.SendAsync(new SubmitCode(@"
#!set --name x --from-result
1+3"));
        var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo(4);
    }

    [Fact]
    public async Task can_set_value_prompting_user()
    {
        using var kernel = CreateKernel(Language.CSharp).UseSet();

        var composite = new CompositeKernel();

        composite.Add(kernel);

        composite.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            context.Publish(new InputProduced("hello!", requestInput));
            return Task.CompletedTask;
        });

        composite.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), composite.Name);


        await composite.SendAsync(new SubmitCode(@"
#!set --name x --from-value @input:input-please
1+3"));
        var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo("hello!");
    }

    [Fact]
    public async Task set_value_using_return_value_fails_when_there_is_no_ReturnValueProduced()
    {
        using var kernel = CreateKernel(Language.CSharp).UseSet();

        var results  = await kernel.SendAsync(new SubmitCode(@"
#!set --name x --from-result
var num = 1+3;"));

        var events = results.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<CommandFailed>()
            .Which.Message.Should().Be("The command was expected to produce a ReturnValueProduced event.");

    }

    [Fact]
    public async Task set_does_not_allow_from_value_and_from_results_at_the_same_time()
    {
        using var kernel = CreateKernel(Language.CSharp).UseSet();

        var results = await kernel.SendAsync(new SubmitCode(@"
#!set --name x --from-result --from-value fsharp:y
1+3"));
        var events = results.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<CommandFailed>()
            .Which.Message.Should().Be("The --from-result and --from-value options cannot be used together.");
    }

    [Fact]
    public async Task set_requires_from_value_or_from_results_at_the_same_time()
    {
        using var kernel = CreateKernel(Language.CSharp).UseSet();

        var results = await kernel.SendAsync(new SubmitCode(@"
#!set --name x
1+3"));
        var events = results.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<CommandFailed>()
            .Which.Message.Should().Be("At least one of the options [from-result, from-value] must be specified.");
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