// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class InputsWithinMagicCommandsTests : IDisposable
{
    private readonly CompositeKernel kernel;

    private RequestInput _receivedRequestInput = null;

    private readonly List<string> _receivedUserInput = new();

    private readonly Command _shimCommand;

    private readonly Queue<string> _responses = new();

    public InputsWithinMagicCommandsTests()
    {
        kernel = CreateKernel();

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            _receivedRequestInput = requestInput;
            context.Publish(new InputProduced(_responses.Dequeue(), requestInput));
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
            _receivedUserInput.Add(context.ParseResult.GetValueForOption(stringOption));
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
    public async Task Input_token_in_magic_command_prompts_user_passes_user_input_to_directive_to_handler()
    {
        _responses.Enqueue("one");

        await kernel.SendAsync(new SubmitCode("#!shim --string @input:input-please", "csharp"));

        _receivedUserInput.Should().ContainSingle().Which.Should().Be("one");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_passes_user_input_to_directive_to_handler_when_there_are_multiple_inputs()
    {
        _responses.Enqueue("one");
        _responses.Enqueue("two");

        await kernel.SendAsync(new SubmitCode("""
            #!shim --string @input:input-please
            #!shim --string @input:input-please
            """, "csharp"));

        _receivedUserInput.Should().BeEquivalentTo("one", "two");
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
    public async Task An_input_type_hint_is_set_for_file_inputs_when_prompt_is_unquoted()
    {
        _shimCommand.Add(new Option<FileInfo>("--file"));

        await kernel.SendAsync(new SubmitCode("""
            #!shim --file @input:file-please
            // some more stuff
            """, "csharp"));

        _receivedRequestInput.InputTypeHint.Should().Be("file");
    }

    [Fact]
    public async Task An_input_type_hint_is_set_for_file_inputs_when_prompt_is_quoted()
    {
        _shimCommand.Add(new Option<FileInfo>("--file"));

        await kernel.SendAsync(new SubmitCode("""
            #!shim --file @input:"file please"
            // some more stuff
            """, "csharp"));

        _receivedRequestInput.InputTypeHint.Should().Be("file");
    }

    [Fact]
    public async Task Unknown_types_return_type_hint_of_text()
    {
        _shimCommand.Add(new Option<CompositeKernel>("--unknown"));

        await kernel.SendAsync(new SubmitCode("#!shim --file @input:file-please\n// some more stuff", "csharp"));

        _receivedRequestInput.InputTypeHint.Should().Be("text");
    }

    [Fact]
    public async Task multiple_set_commands_with_inputs_can_be_used_in_single_submission()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel().UseValueSharing()
        };

        var responses = new Queue<string>();
        responses.Enqueue("one");
        responses.Enqueue("two");
        responses.Enqueue("three");

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            context.Publish(new InputProduced(responses.Dequeue(), requestInput));
            return Task.CompletedTask;
        });

        var result = await kernel.SendAsync(new SubmitCode("""
                #!set --name value1 --value @input:"input-please "
                #!set --name value2 --value @input:"input-please "
                #!set --name value3 --value @input:"input-please"
                """, targetKernelName: "csharp"));

        result.Events.Should().NotContainErrors();

        var csharpKernel = (CSharpKernel)kernel.FindKernelByName("csharp");

        csharpKernel.TryGetValue("value1", out object value1)
                    .Should().BeTrue();
        value1.Should().Be("one");

        csharpKernel.TryGetValue("value2", out object value2)
                    .Should().BeTrue();
        value2.Should().Be("two");

        csharpKernel.TryGetValue("value3", out object value3)
                    .Should().BeTrue();
        value3.Should().Be("three");
    }

    [Fact]
    public async Task additional_properties_of_input_request_are_set_by_input_properties_when_prompt_is_quoted()
    {
        using var kernel = new CSharpKernel();

        var command = new Command("#!test")
        {
            new Argument<string>()
        };
        kernel.AddDirective(command);

        RequestInput requestInput = null;
        kernel.RegisterCommandHandler<RequestInput>((input, context) =>
        {
            requestInput = input;

            return Task.CompletedTask;
        });

        var magicCommand = """#!test @input:"pick a number",save,type=file  """;

        await kernel.SendAsync(new SubmitCode(magicCommand));

        requestInput.Prompt.Should().Be("pick a number");
        // FIX: requestInput.Persistent.Should().BeTrue();
        requestInput.InputTypeHint.Should().Be("file");
    }

    [Fact(Skip = "Evaluating different syntax approaches")]
    public async Task additional_properties_of_input_request_are_set_by_input_properties_when_prompt_or_field_name_is_not_quoted()
    {
        using var kernel = new CSharpKernel();

        var command = new Command("#!test")
        {
            new Argument<string>()
        };
        kernel.AddDirective(command);

        RequestInput requestInput = null;
        kernel.RegisterCommandHandler<RequestInput>((input, context) =>
        {
            requestInput = input;

            return Task.CompletedTask;
        });

        var magicCommand = """#!test @input:promptOrFieldName,save,type=file  """;

        await kernel.SendAsync(new SubmitCode(magicCommand));

        requestInput.Prompt.Should().Be("promptOrFieldName");
        // FIX: requestInput.Persistent.Should().BeTrue();
        requestInput.InputTypeHint.Should().Be("file");
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