// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class KqlDiscoverabilityKernelTests
{

    [Fact]
    public async Task kql_kernel_does_not_execute_query()
    {
        using var kernel = new CompositeKernel
        {
            new KqlDiscoverabilityKernel()
        };

        using var events = kernel.KernelEvents.ToSubscribedList();

        var query = "StormEvents | take 10";
        await kernel.SendAsync(new SubmitCode($"#!kql\n\n{query}"));

        var commandFailed = events.Should()
            .ContainSingle<CommandFailed>()
            .Which;

        var message = commandFailed.Message;

        message.Should()
            .Be("KQL statements cannot be executed in this kernel.");
    }

    [Fact]
    public async Task kql_kernel_suggest_how_to_submit_query()
    {
        using var kernel = new CompositeKernel
        {
            new KqlDiscoverabilityKernel()
        };

        using var events = kernel.KernelEvents.ToSubscribedList();

        var query = "StormEvents | take 10";
        await kernel.SendAsync(new SubmitCode($"#!kql\n\n{query}"));

        var displayValue = events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which;

        var message = displayValue.Value.ToString();

        message.Should()
            .Contain(query);
    }

    [Fact]
    public async Task kql_kernel_emits_help_message_without_sql_server_extension_installed()
    {
        using var kernel = new CompositeKernel
        {
            new KqlDiscoverabilityKernel()
        };

        using var events = kernel.KernelEvents.ToSubscribedList();

        var query = "StormEvents | take 10";
        await kernel.SendAsync(new SubmitCode($"#!kql\n\n{query}"));

        var displayValue = events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which;

        var message = displayValue.Value.ToString();

        // Should contain instructions for how to install SqlServer extension package
        message.Should().Contain("""
                                 #r "nuget:Microsoft.DotNet.Interactive.Kql,1.0.0
                                 """);

        // Should contain instructions for how to get help message for MSSQL kernel
        message.Should().Contain("#!connect kql --kernel-name mydatabase --cluster \"https://help.kusto.windows.net\" --database \"Samples\"");
    }
}