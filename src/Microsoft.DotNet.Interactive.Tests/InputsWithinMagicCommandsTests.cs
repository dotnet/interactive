// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
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
    private readonly Command _shimCommand;

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

        var stringOption = new Option<string>("--string");
        _shimCommand = new("#!shim")
        {
            stringOption
        };
        _shimCommand.SetHandler(context =>
        {
            _receivedUserInput = context.ParseResult.GetValueForOption(stringOption);
        });

        kernel.FindKernelByName("csharp").AddDirective(_shimCommand);
    }

    public void Dispose()
    {
        kernel.Dispose();
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_for_input()
    {
        await kernel.SendAsync(new SubmitCode("#!shim --string @input:input-please", "csharp"));

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
    public async Task Input_token_in_magic_command_includes_requested_value_name()
    {
        await kernel.SendAsync(new SubmitCode("#!shim --string @input:input-please", "csharp"));

        _receivedRequestInput.IsPassword.Should().BeFalse();

        _receivedRequestInput
            .Should()
            .BeOfType<RequestInput>()
            .Which
            .ValueName
            .Should()
            .Be("input-please");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_passes_user_input_to_directive__to_handler()
    {
        await kernel.SendAsync(new SubmitCode("#!shim --string @input:input-please", "csharp"));

        _receivedUserInput.Should().Be("hello!");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_for_password()
    {
        await kernel.SendAsync(new SubmitCode("#!shim --string @password:input-please", "csharp"));

        _receivedRequestInput.IsPassword.Should().BeTrue();

        _receivedRequestInput
            .Should()
            .BeOfType<RequestInput>()
            .Which
            .Prompt
            .Should()
            .Be("Please enter a value for field \"input-please\".");
    }

    [Fact]
    public async Task An_input_type_hint_is_set_for_file_inputs()
    {
        _shimCommand.Add(new Option<FileInfo>("--file"));

        await kernel.SendAsync(new SubmitCode("#!shim --file @input:file-please\n// some more stuff", "csharp"));

        _receivedRequestInput.InputTypeHint.Should().Be("file");
    }

    [Fact]
    public async Task Unknown_types_return_type_hint_of_text()
    {
        _shimCommand.Add(new Option<CompositeKernel>("--unknown"));

        await kernel.SendAsync(new SubmitCode("#!shim --file @input:file-please\n// some more stuff", "csharp"));

        _receivedRequestInput.InputTypeHint.Should().Be("text");
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