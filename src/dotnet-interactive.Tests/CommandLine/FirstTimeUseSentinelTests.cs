// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Xunit;
using CommandLineParser = Microsoft.DotNet.Interactive.App.CommandLine.CommandLineParser;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine;

public class FirstTimeUseSentinelTests : IDisposable
{
    private readonly FileInfo _connectionFile;
    private readonly CompositeDisposable _disposables = new();

    public FirstTimeUseSentinelTests()
    {
        _connectionFile = new FileInfo(Path.GetTempFileName());

        _disposables.Add(() => _connectionFile.Delete());
    }

    private static RootCommand CreateParser(bool sentinelExists)
    {
        var firstTimeUseNoticeSentinel =
            new FirstTimeUseNoticeSentinel(
                "product-version",
                "",
                _ => sentinelExists,
                _ => true,
                _ => { },
                _ => { });

        return CommandLineParser.Create(
            new ServiceCollection(),
            startWebServer: _ => { },
            startJupyter: (_, _) => Task.FromResult(1),
            startStdio: (_, _) => Task.FromResult(1),
            telemetrySender: new FakeTelemetrySender(firstTimeUseNoticeSentinel));
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [Fact]
    public async Task First_time_use_sentinel_does_not_exist_then_print_telemetry_first_time_use_welcome_message()
    {
        var console = new StringWriter();
        var parser = CreateParser(false);
        await parser.Parse($"jupyter {_connectionFile}").InvokeAsync(new() { Output = console });
        Assert.Contains("Telemetry", console.ToString());
    }

    [Fact]
    public async Task First_time_use_sentinel_exists_then_do_not_print_telemetry_first_time_use_welcome_message()
    {
        var console = new StringWriter();
        var parser = CreateParser(true);
        await parser.Parse($"jupyter  {_connectionFile}").InvokeAsync(new() { Output = console });
        Assert.DoesNotContain("Telemetry", console.ToString());
    }
}