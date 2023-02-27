// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using Pocket.For.Xunit;
using static Pocket.Logger<Microsoft.DotNet.Interactive.App.Tests.StdioConnectionTests>;

namespace Microsoft.DotNet.Interactive.App.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class StdioConnectionTests : ProxyKernelConnectionTestsBase
{
    private readonly StdioConnectionTestConfiguration _configuration;

    public StdioConnectionTests(ITestOutputHelper output) : base(output)
    {
        _configuration = CreateConnectionConfiguration();
    }

    protected override Task<IKernelConnector> CreateConnectorAsync()
    {
        var command = new List<string> { _configuration.Command };

        if (_configuration.Args?.Length > 0)
        {
            command.AddRange(_configuration.Args);
        }

        var connector = new StdIoKernelConnector(
            command.ToArray(),
            kernelHostUri: new Uri("kernel://test-kernel"),
            workingDirectory: _configuration.WorkingDirectory);

        RegisterForDisposal(connector);

        return Task.FromResult<IKernelConnector>(connector);
    }

    protected StdioConnectionTestConfiguration CreateConnectionConfiguration()
    {
        var toolPath = new FileInfo(typeof(Program).Assembly.Location);

        var args = new List<string>
        {
            $"\"{toolPath.FullName}\"",
            "stdio",
            "--default-kernel",
            "csharp",
        };

#if DEBUG
        if (Environment.GetEnvironmentVariable("POCKETLOGGER_LOG_PATH") is { } logPath)
        {
            var fileInfo = new FileInfo(logPath);
            var proxiedLogPath = Path.Combine(fileInfo.DirectoryName, $"proxied.{fileInfo.Name}");

            args.Add("--verbose");
            args.Add("--log-path");
            args.Add(proxiedLogPath);
        }
#endif

        return new StdioConnectionTestConfiguration
        {
            Command = Dotnet.Path.FullName,
            Args = args.ToArray(),
            WorkingDirectory = toolPath.Directory
        };
    }

    [Fact]
    public async Task stdio_server_encoding_is_utf_8()
    {
        using var localCompositeKernel = new CompositeKernel
        {
            new FakeKernel("fsharp")
        };

        AddKernelConnector(localCompositeKernel);

        localCompositeKernel.DefaultKernelName = "fsharp";

        var connectToRemoteKernel = new SubmitCode(
            $"#!connect stdio --kernel-name newKernelName --command \"{_configuration.Command}\" {string.Join(" ", _configuration.Args)}");

        await localCompositeKernel.SendAsync(connectToRemoteKernel);

        var result = await localCompositeKernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName", "newKernelName"));
        
        var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

        result.Events
           .Should()
           .EventuallyContainSingle<DisplayEvent>(
               where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected),
               timeout: 10_000);
    }

    [Fact]
    public async Task issue_2726()
    {
        var workingDirectoryPath = @"c:\temp\deadlocks";

        // I installed the latest dotnet-interactive tool in the above directory by running the following commands
        // dotnet new tool-manifest
        // dotnet tool install Microsoft.dotnet-interactive --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" --version 1.0.410905

        var connector = new StdIoKernelConnector(
            new[] { "dotnet", """
                C:\dev\interactive\src\dotnet-interactive\bin\Debug\net7.0\Microsoft.DotNet.Interactive.App.dll stdio --default-kernel csharp --verbose --log-path "c:\temp\testlogs"
                """ },
            KernelHost.CreateHostUri("VS"),
            new DirectoryInfo(workingDirectoryPath));

        var kernel = 
            new CompositeKernel("LocalComposite")
            {
                await connector.CreateKernelAsync("proxy")
            };

        var events = kernel.KernelEvents.Subscribe(e =>
        {
            Log.Info(e.ToDisplayString());
        });

        // await kernel.SendAsync(new SubmitCode("System.Diagnostics.Debugger.Launch();"));

        await Task.Delay(2000);

        await kernel.SendAsync(new RequestKernelInfo());

        Log.Info(kernel.KernelInfo.ToDisplayString("text/plain"));
        

        // TODO (testname) write test
        throw new NotImplementedException();
    }

    protected override SubmitCode CreateConnectCommand(string localKernelName)
    {
        return new SubmitCode(
            $"#!connect stdio --kernel-name {localKernelName} --command \"{_configuration.Command}\" {string.Join(" ", _configuration.Args)}");
    }

    protected override void AddKernelConnector(CompositeKernel compositeKernel)
    {
        compositeKernel.AddKernelConnector(new ConnectStdIoCommand(new Uri("kernel://test-kernel")));
    }
}

public class StdioConnectionTestConfiguration
{
    public string Command { get; set; }

    public string[] Args { get; set; }
    
    public DirectoryInfo WorkingDirectory { get; set; }
}