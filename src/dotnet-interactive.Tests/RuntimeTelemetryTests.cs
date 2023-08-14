// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class RuntimeTelemetryTests : IDisposable
{
    private readonly FakeTelemetrySender _telemetrySender;
    private readonly CompositeKernel _kernel;

    public RuntimeTelemetryTests()
    {
        _telemetrySender = new FakeTelemetrySender();
        _kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(false)
            }
            .UseTelemetrySender(_telemetrySender);
    }

    public void Dispose() => _kernel.Dispose();

    [Fact]
    public async Task Language_information_is_sent_on_successful_code_execution_when_target_kernel_is_specified()
    {
        var telemetrySender = new FakeTelemetrySender();
        using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("kql", "KQL"),
                new FakeKernel("sql", "T-SQL")
            }
            .UseTelemetrySender(telemetrySender);

        await kernel.SendAsync(new SubmitCode(@"
#!sql
select * from db

#!kql
telemetry
| take 1

#!csharp
1+1
"));

        var properties = telemetrySender.TelemetryEvents.Where(e => e.EventName == "CodeSubmitted").SelectMany(e => e.Properties).Where(p => p.Key is "KernelName" or "KernelLanguageName").ToArray();

        var expected = new[]
        {
            new KeyValuePair<string, string>("KernelName", "kql".ToSha256Hash()),
            new KeyValuePair<string, string>("KernelLanguageName", "KQL".ToSha256Hash()),
            new KeyValuePair<string, string>("KernelName", "sql".ToSha256Hash()),
            new KeyValuePair<string, string>("KernelLanguageName", "T-SQL".ToSha256Hash()),
            new KeyValuePair<string, string>("KernelName", "csharp".ToSha256Hash()),
            new KeyValuePair<string, string>("KernelLanguageName", "C#".ToSha256Hash())
        };

        properties.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Language_information_is_sent_on_successful_code_execution_after_split()
    {
        await _kernel.SendAsync(new SubmitCode("123", "csharp"));

        _telemetrySender.TelemetryEvents
            .Should()
            .ContainSingle(e => e.EventName == "CodeSubmitted")
            .Which
            .Properties
            .Should()
            .Contain(
                new KeyValuePair<string, string>("KernelName", "csharp".ToSha256Hash()),
                new KeyValuePair<string, string>("KernelLanguageName", "C#".ToSha256Hash()));
    }

    [Fact]
    public async Task Language_information_is_sent_on_unsuccessful_code_execution_when_target_kernel_is_specified()
    {
        await _kernel.SendAsync(new SubmitCode("that doesn't compile", "csharp"));

        _telemetrySender.TelemetryEvents
                        .Should()
                        .ContainSingle(e => e.EventName == "CodeSubmitted")
                        .Which
                        .Properties
                        .Should()
                        .Contain(new KeyValuePair<string, string>("KernelName", "csharp".ToSha256Hash()));
    }

    [Fact]
    public async Task Language_information_is_sent_on_successful_code_execution_when_target_kernel_is_not_specified()
    {
        await _kernel.SendAsync(new SubmitCode("123"));

        _telemetrySender.TelemetryEvents
                        .Should()
                        .ContainSingle(e => e.EventName == "CodeSubmitted")
                        .Which
                        .Properties
                        .Should()
                        .Contain(
                            new KeyValuePair<string, string>("KernelName", "csharp".ToSha256Hash()),
                            new KeyValuePair<string, string>("KernelLanguageName", "C#".ToSha256Hash()));
    }

    [Fact]
    public async Task Language_information_is_sent_on_unsuccessful_code_execution_when_target_kernel_is_not_specified()
    {
        await _kernel.SendAsync(new SubmitCode("that doesn't compile"));

        _telemetrySender.TelemetryEvents
                        .Should()
                        .ContainSingle(e => e.EventName == "CodeSubmitted")
                        .Which
                        .Properties
                        .Should()
                        .Contain(new KeyValuePair<string, string>("KernelName", "csharp".ToSha256Hash()));
    }

    [Fact]
    public async Task Kernel_session_id_is_sent()
    {
        await _kernel.SendAsync(new SubmitCode("123"));
        await _kernel.SendAsync(new SubmitCode("456"));

        var sessionIdForFirstExecution = _telemetrySender.TelemetryEvents[0].Properties["KernelSessionId"];
        var sessionIdForSecondExecution = _telemetrySender.TelemetryEvents[1].Properties["KernelSessionId"];

        sessionIdForFirstExecution
            .Should()
            .Be(sessionIdForSecondExecution);
    }

    [Fact]
    public async Task Package_and_version_number_are_sent_on_successful_package_load()
    {
        var results = await _kernel.SendAsync(new SubmitCode("#r \"nuget:NodaTime,3.1.9\"", "csharp"));

        results.Events.Should().NotContainErrors();

        _telemetrySender.TelemetryEvents
                        .Should()
                        .ContainSingle(e => e.EventName == "PackageAdded")
                        .Which
                        .Properties
                        .Should()
                        .Contain(
                            new KeyValuePair<string, string>("PackageName", "nodatime".ToSha256HashWithNormalizedCasing()),
                            new KeyValuePair<string, string>("PackageVersion", "3.1.9".ToSha256Hash()));
    }
}