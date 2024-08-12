// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell;
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

    private static CompositeKernel CreateKernel()
    {
        var kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new PowerShellKernel(),
            new KeyValueStoreKernel()
        }.UseSecretManager();

        kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);

        return kernel;
    }
}