// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class InputsWithinMagicCommandsTests : IDisposable
{
    private readonly CompositeKernel kernel;
    private RequestInput _receivedRequestInput = null;
    private string _receivedUserInput = null;

    public InputsWithinMagicCommandsTests()
    {
        kernel = CreateKernel();

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            _receivedRequestInput = requestInput;
            context.Publish(new InputProduced("hello!", requestInput));
            return Task.CompletedTask;
        });

        kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);

        var argument = new Argument<string>();
        Command shim = new("#!shim")
        {
            argument
        };
        shim.SetHandler((string value) => { _receivedUserInput = value; }, argument);

        kernel.FindKernel("csharp").AddDirective(shim);
    }

    public void Dispose()
    {
        kernel.Dispose();
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_for_input()
    {
        await kernel.SendAsync(new SubmitCode("#!shim @input:input-please", "csharp"));

        _receivedRequestInput.IsPassword.Should().BeFalse();

        _receivedRequestInput
            .Should()
            .BeOfType<RequestInput>()
            .Which
            .Prompt
            .Should()
            .Be("Please enter a value for field \"input-please\".");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_passes_user_input_to_directive__to_handler()
    {
        await kernel.SendAsync(new SubmitCode("#!shim @input:input-please", "csharp"));

        _receivedUserInput.Should().Be("hello!");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_for_password()
    {
        await kernel.SendAsync(new SubmitCode("#!shim @password:input-please", "csharp"));

        _receivedRequestInput.IsPassword.Should().BeTrue();

        _receivedRequestInput
            .Should()
            .BeOfType<RequestInput>()
            .Which
            .Prompt
            .Should()
            .Be("Please enter a value for field \"input-please\".");
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