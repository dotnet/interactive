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
    public async Task can_set_value_prompting_user()
    {
        var kernel = CreateKernel(Language.CSharp);

        using var composite = new CompositeKernel();

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
    public async Task can_set_value_from_another_kernel()
    {
        var csharpKernel = CreateKernel(Language.CSharp);
        var fsharpKernel = CreateKernel(Language.FSharp);

        using var composite = new CompositeKernel
        {
            csharpKernel,
            fsharpKernel
        };

        await fsharpKernel.SendAsync(new SubmitCode("let y = 456"));

        await composite.SendAsync(new SubmitCode($@"
#!set --name x --from-value @{fsharpKernel.Name}:y
1+3", targetKernelName: csharpKernel.Name));

        var (succeeded, valueProduced) = await csharpKernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo(456);
    }

    [Fact]
    public async Task when_mimetype_is_specified_reference_value_is_ignored()
    {
        var csharpKernel = CreateKernel(Language.CSharp);
        var fsharpKernel = CreateKernel(Language.FSharp);

        using var composite = new CompositeKernel
        {
            csharpKernel,
            fsharpKernel
        };

        await fsharpKernel.SendAsync(new SubmitCode("let y = 456"));

        await composite.SendAsync(new SubmitCode($@"
#!set --name x --from-value @{fsharpKernel.Name}:y --mime-type text/plain
1+3", targetKernelName: csharpKernel.Name));

        var (succeeded, valueProduced) = await csharpKernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo("456");
    }


    [Fact]
    public async Task set_requires_from_option()
    {
        using var kernel = CreateKernel(Language.CSharp);

        var results = await kernel.SendAsync(new SubmitCode(@"
#!set --name x
1+3"));
        var events = results.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<CommandFailed>()
            .Which.Message.Should().Be("Option '--from-value' is required.");
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
