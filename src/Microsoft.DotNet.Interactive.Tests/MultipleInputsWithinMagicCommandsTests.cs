// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class MultipleInputsWithinMagicCommandsTests : IDisposable
{
    private readonly CompositeKernel _kernel;
    private readonly KernelActionDirective _shimCommand;
    private readonly List<ShimCommand> _receivedShimCommands = new();
    private readonly SecretManager _secretManager;

    public MultipleInputsWithinMagicCommandsTests()
    {
        var powerShellKernel = new PowerShellKernel();

        _secretManager = new SecretManager(powerShellKernel);

        _kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            powerShellKernel, 
            new KeyValueStoreKernel()
        }.UseFormsForMultipleInputs(_secretManager);

        _shimCommand = new("#!shim")
        {
            KernelCommandType = typeof(ShimCommand),
            Parameters =
            {
                new("--name"), 
                new("--value")
                {
                    AllowImplicitName = true
                },
                new("--another-value")
            }
        };

        _kernel.FindKernelByName("csharp")
               .AddDirective<ShimCommand>(_shimCommand, (command, _) =>
               {
                   _receivedShimCommands.Add(command);
                   return Task.CompletedTask;
               });
    }

    public class ShimCommand : KernelCommand
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string AnotherValue { get; set; }
    }

    public void Dispose()
    {
        _kernel.Dispose();
    }

    [Fact]
    public async Task Multiple_inputs_are_bound_within_a_single_magic_command_that_uses_JSON_binding()
    {
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "age",
            ["value"] = "123",
            ["anotherValue"] = "456",
            ["fileValue"] = @"c:\temp\some-file.txt",
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode("""
                                        #!shim --name @input --value @input:{"type": "date"} --another-value 456
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        using var _ = new AssertionScope();

        var receivedCommand=  _receivedShimCommands.Should().ContainSingle().Which;
        receivedCommand.Name.Should().Be("age");
        receivedCommand.Value.Should().Be("123");
        receivedCommand.AnotherValue.Should().Be("456");
    }

    [Fact]
    public async Task Multiple_inputs_are_bound_within_a_single_magic_command_that_uses_custom_binding()
    {
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "age",
            ["value"] = "123"
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode("""
                                        #!set --name @input:age --value @input:{"type": "number"}
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        _kernel.FindKernelByName("csharp").As<CSharpKernel>().TryGetValue<string>("age", out var boundValue);

        boundValue.Should().Be("123");
    }

    [Fact]
    public async Task An_input_type_hint_is_set_when_the_expected_parameter_specifies_it()
    {
        _shimCommand.Parameters.Add(new KernelDirectiveParameter("--file")
        {
            TypeHint = "file"
        });

        RequestInputs requestInputsSent = null;
        _kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is RequestInputs requestInput)
            {
                requestInputsSent = requestInput;
            }

            await next(command, context);
        });

        var fileValue = @"c:\temp\some-file.txt";
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "theFile",
            ["file"] = fileValue
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode("""
                                        #!shim --name @input --file @input
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        requestInputsSent.Inputs.Where(description => description.Name == "--file")
                         .Should().ContainSingle()
                         .Which
                         .TypeHint.Should()
                         .Be("file");
    }

    [Fact]
    public async Task Type_hint_is_set_based_on_inline_JSON_configuration_of_the_input_token()
    {
        RequestInputs requestInputsSent = null;
        _kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is RequestInputs requestInput)
            {
                requestInputsSent = requestInput;
            }

            await next(command, context);
        });

        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "theFile",
            ["date"] = "2022-01-01"
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode("""
                                        #!shim --name @input --value @input{"type": "date"}
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        requestInputsSent.Inputs.Where(description => description.Name == "--value")
                         .Should().ContainSingle()
                         .Which
                         .TypeHint.Should()
                         .Be("date");
    }

    [Fact]
    public async Task Type_hint_is_overridden_based_on_inline_JSON_configuration_of_the_input_token()
    {
        _shimCommand.Parameters.Add(new KernelDirectiveParameter("--file")
        {
            TypeHint = "file"
        });

        RequestInputs requestInputsSent = null;
        _kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is RequestInputs requestInput)
            {
                requestInputsSent = requestInput;
            }

            await next(command, context);
        });

        var fileValue = @"c:\temp\some-file.txt";
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "theFile",
            ["file"] = fileValue
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode("""
                                        #!shim --name @input --file @input{"type": "date"}
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        requestInputsSent.Inputs.Where(description => description.Name == "--file")
                         .Should().ContainSingle()
                         .Which
                         .TypeHint.Should()
                         .Be("date");
    }

    [Fact]
    public async Task Input_field_values_can_be_stored_using_SecretManager()
    {
        // make the secret name unique across runs
        var secretName = nameof(Previously_stored_values_are_used_to_prepopulate_input_fields) + DateTime.UtcNow.Ticks;

        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "age",
            ["value"] = "123"
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode($$"""
                                          #!set --name @input --value @input:{"saveAs": "{{secretName}}"}
                                          """, "csharp"));

        result.Events.Should().NotContainErrors();

        _secretManager.TryGetValue(secretName, out var storedValue).Should().BeTrue();

        storedValue.Should().Be("123");
    }

    [Fact]
    public async Task Previously_stored_values_are_used_to_prepopulate_input_fields()
    {
        // make the secret name unique across runs
        var secretName = nameof(Previously_stored_values_are_used_to_prepopulate_input_fields) + DateTime.UtcNow.Ticks;

        var theStoredValue = "the stored value";
        _secretManager.SetValue(name: secretName, value: theStoredValue);

        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "age",
            ["value"] = "123"
        });

        using var events = _kernel.KernelEvents.ToSubscribedList();

        await _kernel.SendAsync(
            new SubmitCode($$"""
                             #!shim --name @input --value @input:{"saveAs": "{{secretName}}"}
                             """, "csharp"));

        events.Should().NotContainErrors();

        events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle()
              .Which
              .Value
              .Should()
              .Match($"*<input * name=\"value\" value=\"{theStoredValue}\"*");
    }

    [Fact]
    public async Task When_multiple_inputs_are_enabled_then_RequestInput_is_not_sent_for_a_magic_command_that_uses_custom_binding()
    {
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "fruit",
            ["value"] = "cherry"
        });

        RequestInput requestInputSent = null;
        _kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is RequestInput requestInput)
            {
                requestInputSent = requestInput;
            }

            await next(command, context);
        });

        await _kernel.SendAsync(
            new SubmitCode("""
                           #!set --name @input:name --value @input:value
                           """, "csharp"));

        requestInputSent.Should().BeNull();
    }

    [Fact]
    public async Task When_multiple_inputs_are_enabled_then_RequestInput_is_not_sent_for_a_magic_command_that_uses_JSON_binding()
    {
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "fruit",
            ["value"] = "cherry"
        });

        RequestInput requestInputSent = null;
        _kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is RequestInput requestInput)
            {
                requestInputSent = requestInput;
            }

            await next(command, context);
        });

        await _kernel.SendAsync(
            new SubmitCode("""
                           #!shim --name @input --value @input
                           """, "csharp"));

        requestInputSent.Should().BeNull();
    }

    [Fact]
    public async Task When_RequestInputs_is_not_supported_then_it_falls_back_to_sending_multiple_RequestInput_commands()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new KeyValueStoreKernel()
        };
        List<RequestInput> receivedRequestInputs = [];
        Queue<string> responses = new();
        responses.Enqueue("one");
        responses.Enqueue("two");

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            receivedRequestInputs.Add(requestInput);
            context.Publish(new InputProduced(responses.Dequeue(), requestInput));
            return Task.CompletedTask;
        });

        kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), _kernel.Name);

        var result = await kernel.SendAsync(
                         new SubmitCode("""
                                        #!set --name @input --value @password
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        using var _ = new AssertionScope();

        receivedRequestInputs.Should().HaveCount(2);
        receivedRequestInputs[0].ParameterName.Should().Be("--name");
        receivedRequestInputs[1].ParameterName.Should().Be("--value");
    }

    [Theory]
    [MemberData(nameof(LanguageServiceCommands))]
    public async Task Language_service_commands_do_not_trigger_input_requests(KernelCommand command)
    {
        var result = await _kernel.SendAsync(command);
        
        _kernel.RespondToRequestInputsFormWith(new Dictionary<string, string>
        {
            ["name"] = "fruit",
            ["value"] = "cherry"
        });

        RequestInputs requestInputsSent = null;
        _kernel.AddMiddleware(async (kernelCommand, context, next) =>
        {
            if (kernelCommand is RequestInputs requestInput)
            {
                requestInputsSent = requestInput;
            }

            await next(command, context);
        });

        result.Events.Should().NotContainErrors();

        requestInputsSent.Should().BeNull();
    }

    public static IEnumerable<object[]> LanguageServiceCommands()
    {
        var code = "#!set --name @input:name --value @password:password";

        yield return [new RequestCompletions(code, new LinePosition(0, code.Length), targetKernelName: "csharp")];
        yield return [new RequestHoverText(code, new LinePosition(0, 3), targetKernelName: "csharp")];
        yield return [new RequestDiagnostics(code, targetKernelName: "csharp")];
        yield return [new RequestSignatureHelp(code, new LinePosition(0, 3), targetKernelName: "csharp")];
        
        code = "#!shim --name @input:name --value @password:password";

        yield return [new RequestCompletions(code, new LinePosition(0, code.Length), targetKernelName: "csharp")];
        yield return [new RequestHoverText(code, new LinePosition(0, 3), targetKernelName: "csharp")];
        yield return [new RequestDiagnostics(code, targetKernelName: "csharp")];
        yield return [new RequestSignatureHelp(code, new LinePosition(0, 3), targetKernelName: "csharp")];
    }
}