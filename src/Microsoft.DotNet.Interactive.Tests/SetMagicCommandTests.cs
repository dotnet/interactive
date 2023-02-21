// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Equivalency;
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


        await composite.SendAsync(new SubmitCode("#!set --name x --value @input:input-please"));
        var (succeeded, valueProduced) = await kernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo("hello!");
    }

    [Fact]
    public async Task can_set_scalar_value_from_another_kernel()
    {
        var csharpKernel = CreateKernel(Language.CSharp);
        var fsharpKernel = CreateKernel(Language.FSharp);

        using var composite = new CompositeKernel
        {
            csharpKernel,
            fsharpKernel
        };

        await fsharpKernel.SendAsync(new SubmitCode("let y = 456"));

        await composite.SendAsync(new SubmitCode($"#!set --name x --value @{fsharpKernel.Name}:y", targetKernelName: csharpKernel.Name));

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

        await composite.SendAsync(new SubmitCode($"#!set --name x --value @{fsharpKernel.Name}:y --mime-type text/plain", targetKernelName: csharpKernel.Name));

        var (succeeded, valueProduced) = await csharpKernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        succeeded.Should().BeTrue();
        valueProduced.Value.Should().BeEquivalentTo("456");
    }

    [Fact]
    public async Task honors_mimetype_from_value_kernel()
    {
        var csharpKernel = CreateKernel(Language.CSharp);
        var valueKernel = new KeyValueStoreKernel().UseValueSharing();

        using var composite = new CompositeKernel
        {
            csharpKernel,
            valueKernel
        };
        var jsonFragment = @"{
    ""a"" : 123
}";
        await composite.SendAsync(new SubmitCode(@$"#!value --name data --mime-type application/json
{jsonFragment}
"));

        var result = await composite.SendAsync(new SubmitCode($@"#!set --name x --value @{valueKernel.Name}:data ", targetKernelName: csharpKernel.Name));

        result.Events.Should().NotContainErrors();
        var (succeeded, valueProduced) = await csharpKernel.TryRequestValueAsync("x");

        using var _ = new AssertionScope();

        var expected = JsonDocument.Parse(jsonFragment);
        succeeded.Should().BeTrue();
        valueProduced.Value.Should()
            .BeOfType<JsonDocument>()
            .Which
            .Should()
            .BeEquivalentTo(expected, JsonEquivalenceConfig);

        static EquivalencyAssertionOptions<JsonDocument> JsonEquivalenceConfig(EquivalencyAssertionOptions<JsonDocument> opt) => opt.ComparingByMembers<JsonElement>();

    }

    [Fact]
    public async Task name_option_is_required()
    {
        using var kernel = CreateKernel(Language.CSharp);

        var results = await kernel.SendAsync(new SubmitCode("#!set --value x"));

        results.Events.Should().ContainSingle<CommandFailed>()
               .Which.Message.Should().Be("Option '--name' is required.");
    }

    [Fact]
    public async Task value_option_is_required()
    {
        using var kernel = CreateKernel(Language.CSharp);

        var results = await kernel.SendAsync(new SubmitCode("#!set --name x"));

        results.Events.Should().ContainSingle<CommandFailed>()
               .Which.Message.Should().Be("Option '--value' is required.");
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