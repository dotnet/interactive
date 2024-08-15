// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

public class InputsWithinMagicCommandsTests : IDisposable
{
    private readonly CompositeKernel _kernel;

    private RequestInput _receivedRequestInput = null;

    private readonly List<string> _receivedUserInput = new();

    private readonly KernelActionDirective _shimCommand;

    private readonly Queue<string> _responses = new();

    public InputsWithinMagicCommandsTests()
    {
        _kernel = CreateKernel();

        _kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            _receivedRequestInput = requestInput;
            context.Publish(new InputProduced(_responses.Dequeue(), requestInput));
            return Task.CompletedTask;
        });

        _kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), _kernel.Name);

        _shimCommand = new("#!shim")
        {
            KernelCommandType = typeof(ShimCommand),
            Parameters =
            {
                new("--value")
            }
        };

        _kernel.FindKernelByName("csharp")
               .AddDirective<ShimCommand>(_shimCommand, (command, _) =>
               {
                   _receivedUserInput.Add(command.Value);
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
    public async Task Input_token_in_magic_command_prompts_user_for_input()
    {
        await _kernel.SendAsync(new SubmitCode("#!shim --value @input:input-please", "csharp"));

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
    public async Task Input_token_in_magic_command_prompts_user_passes_user_input_to_directive_to_handler()
    {
        _responses.Enqueue("one");

        var result = await _kernel.SendAsync(new SubmitCode("#!shim --value @input:input-please", "csharp"));

        result.Events.Should().NotContainErrors();
        _receivedUserInput.Should().ContainSingle().Which.Should().Be("one");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_passes_user_input_to_directive_to_handler_when_there_are_multiple_inputs()
    {
        _responses.Enqueue("one");
        _responses.Enqueue("two");

        var result = await _kernel.SendAsync(new SubmitCode("""
            #!shim --value @input:input-please
            #!shim --value @input:input-please
            """, "csharp"));

        result.Events.Should().NotContainErrors();
        _receivedUserInput.Should().BeEquivalentTo("one", "two");
    }

    [Fact]
    public async Task Input_token_in_magic_command_prompts_user_for_password()
    {
        await _kernel.SendAsync(new SubmitCode("#!shim --value @password:input-please", "csharp"));

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
    public async Task An_input_type_hint_is_set_when_the_expected_parameter_specifies_it()
    {
        _shimCommand.Parameters.Add(new KernelDirectiveParameter("--file")
        {
            TypeHint = "file"
        });

        await _kernel.SendAsync(new SubmitCode("""
            #!shim --file @input:"file please"
            // some more stuff
            """, "csharp"));

        _receivedRequestInput.InputTypeHint.Should().Be("file");
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
    public async Task Additional_properties_of_input_request_are_set_by_input_properties()
    {
        using var kernel = new CSharpKernel();

        var command = new KernelActionDirective("#!test")
        {
            KernelCommandType = typeof(TestCommand),
            Parameters =
            {
                new("--value")
                {
                    AllowImplicitName = true
                }
            }
        };

        RequestInput requestInput = null;
        kernel.AddDirective<TestCommand>(command, (_, _) => Task.CompletedTask);

        kernel.RegisterCommandHandler<RequestInput>((input, _) =>
        {
            requestInput = input;

            return Task.CompletedTask;
        });

        var magicCommand = """
            #!test @input:{ "prompt": "pick a number", "saveAs": "this-is-the-save-key", "type": "file" } 
            """;

        await kernel.SendAsync(new SubmitCode(magicCommand));

        requestInput.Prompt.Should().Be("pick a number");
        requestInput.SaveAs.Should().Be("this-is-the-save-key");
        requestInput.InputTypeHint.Should().Be("file");
    }

    [Theory]
    [MemberData(nameof(LanguageServiceCommands))]
    public async Task Language_service_commands_do_not_trigger_input_requests(KernelCommand command)
    {
        using var kernel = new CSharpKernel().UseValueSharing();

        bool requestInputWasSent = false;

        kernel.RegisterCommandHandler<RequestInput>((input, _) =>
        {
            requestInputWasSent = true;

            return Task.CompletedTask;
        });

        var result = await kernel.SendAsync(command);

        result.Events.Should().NotContainErrors();

        requestInputWasSent.Should().BeFalse();
    }

    public static IEnumerable<object[]> LanguageServiceCommands()
    {
        // Testing with both one and multiple inputs in a single magic command
        var code = "#!set --name @input:name --value 123";

        yield return [new RequestCompletions(code, new LinePosition(0, code.Length))];
        yield return [new RequestHoverText(code, new LinePosition(0, 3))];
        yield return [new RequestDiagnostics(code)];
        yield return [new RequestSignatureHelp(code, new LinePosition(0, 3))];
        
        code = "#!set --name @input:name --value @password:password ";

        yield return [new RequestCompletions(code, new LinePosition(0, code.Length))];
        yield return [new RequestHoverText(code, new LinePosition(0, 3))];
        yield return [new RequestDiagnostics(code)];
        yield return [new RequestSignatureHelp(code, new LinePosition(0, 3))];
    }

    internal class TestCommand : KernelCommand
    {
        public string Value { get; set; }
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