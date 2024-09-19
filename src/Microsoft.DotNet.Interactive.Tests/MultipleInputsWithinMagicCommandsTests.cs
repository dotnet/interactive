// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class MultipleInputsWithinMagicCommandsTests : IDisposable
{
    private readonly CompositeKernel _kernel;
    private readonly KernelActionDirective _shimCommand;
    private readonly List<ShimCommand> _receivedShimCommands = new();

    public MultipleInputsWithinMagicCommandsTests()
    {
        _kernel = CreateKernel();

        _shimCommand = new("#!shim")
        {
            KernelCommandType = typeof(ShimCommand),
            Parameters =
            {
                new("--name"), 
                new("--value")
                {
                    AllowImplicitName = true
                }
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
            ["value"] = "123"
        });

        var result = await _kernel.SendAsync(
                         new SubmitCode("""
                                        #!shim --name @input --value @input:{"type": "date"}
                                        """, "csharp"));

        result.Events.Should().NotContainErrors();

        using var _ = new AssertionScope();

        var receivedCommand=  _receivedShimCommands.Should().ContainSingle().Which;
        receivedCommand.Name.Should().Be("age");
        receivedCommand.Value.Should().Be("123");
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

    private static CompositeKernel CreateKernel() =>
        new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new KeyValueStoreKernel()
        }.UseFormsForMultipleInputs();
}