// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class RequestInputTests
{
    [Fact]
    public async Task When_Save_is_specified_then_subsequent_requests_reuse_the_saved_value()
    {
        var inputRequestCount = 0;
        var kernel = CreateKernel();

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            inputRequestCount++;
            context.Publish(new InputProduced($"Response #{inputRequestCount}", requestInput));
            return Task.CompletedTask;
        });

        var saveAs = nameof(When_Save_is_specified_then_subsequent_requests_reuse_the_saved_value) + DateTime.Now.Ticks;

        await kernel.SendAsync(new RequestInput("Enter a value")
        {
            SaveAs = saveAs
        });

        await kernel.SendAsync(new RequestInput("Enter a value")
        {
            SaveAs = saveAs
        });

        inputRequestCount.Should().Be(1);
    }

    [Fact]
    public async Task When_a_value_is_saved_then_the_user_is_notified()
    {
        var kernel = CreateKernel();

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            context.Publish(new InputProduced("hello!", requestInput));
            return Task.CompletedTask;
        });

        var saveAs = nameof(When_a_value_is_saved_then_the_user_is_notified) + DateTime.Now.Ticks;

        var valueName = "the-name-of-the-value";

        var result = await kernel.SendAsync(new RequestInput("Enter a value", valueName)
        {
            SaveAs = saveAs
        });

        result.Events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle()
              .Which
              .Value
              .Should()
              .Match($"Your response for value `{saveAs}` has been saved and will be reused without a prompt in the future.*To remove this value *, run the following command in a PowerShell cell:*");
    }

    [Fact]
    public async Task When_a_saved_value_is_used_then_the_user_is_notified()
    {
        var kernel = CreateKernel();
        var secretManager = new SecretManager(kernel.ChildKernels.OfType<PowerShellKernel>().Single());

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            context.Publish(new InputProduced("hello", requestInput));
            return Task.CompletedTask;
        });

        var saveAs = nameof(When_a_saved_value_is_used_then_the_user_is_notified) + DateTime.Now.Ticks;

        secretManager.SetSecret(saveAs, "the-value");

        var result = await kernel.SendAsync(new RequestInput("Enter a value")
        {
            SaveAs = saveAs
        });

        result.Events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle()
              .Which
              .Value
              .Should()
              .Match($"Using previously saved value for `{saveAs}`.*To remove this value *, run the following command in a PowerShell cell:*");
    }

    [Fact]
    public async Task Multiple_inputs_can_be_requested_together_using_command()
    {
        var requestInputs = new RequestInputs
        {
            Inputs =
            [
                new("Fruit"),
                new("Tastiness"),
                new("Color")
            ]
        };

        using var kernel = CreateKernel()
            .UseFormsForMultipleInputs();

        var formValues = new Dictionary<string, string>
        {
            ["Fruit"] = "cherry",
            ["Tastiness"] = "9000",
            ["Color"] = "red"
        };

        kernel.RespondToRequestInputsFormWith(formValues);

        var result = await kernel.SendAsync(requestInputs);

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<InputsProduced>()
              .Which.Values.Should().BeEquivalentTo(formValues);
    }

    [Fact]
    public async Task Multiple_inputs_can_be_requested_together_using_GetInputsAsync()
    {
        using var kernel = CreateKernel()
            .UseFormsForMultipleInputs();

        var formValues = new Dictionary<string, string>
        {
            ["Fruit"] = "cherry",
            ["Tastiness"] = "9000",
            ["Color"] = "red"
        };

        kernel.RespondToRequestInputsFormWith(formValues);

        var result = await kernel.SendAsync(
                         new SubmitCode(
                             """
                             using Microsoft.DotNet.Interactive;

                             var inputResult = await Kernel.GetInputsAsync(
                             [
                                 new("Fruit"),
                                 new("Tastiness"),
                                 new("Color")
                             ]);
                             """, "csharp"));

        result.Events.Should().NotContainErrors();

        var csharpKernel = (CSharpKernel)kernel.FindKernelByName("csharp");

        csharpKernel.TryGetValue<IDictionary<string, string>>("inputResult", out var inputResult).Should().BeTrue();

        inputResult.Should().BeEquivalentTo(formValues);
    }

    private static CompositeKernel CreateKernel()
    {
        var powerShellKernel = new PowerShellKernel();

        var kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            powerShellKernel,
            new KeyValueStoreKernel()
        }.UseSecretManager(new SecretManager(powerShellKernel));

        kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);

        return kernel;
    }
}