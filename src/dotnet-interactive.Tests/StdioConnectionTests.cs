// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using Xunit.Abstractions;
using Pocket.For.Xunit;
using Serilog;

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
        var kernel = await CreateKernelAsync();

        await kernel.SendAsync(new RequestKernelInfo());

        // TODO (testname) write test
        throw new NotImplementedException();
    }

    private static async Task<Kernel> CreateKernelAsync()
    {
        var connector = CreateConnector();

        var kernel = await connector.CreateKernelAsync("proxy");

        return kernel;
    }

    private static StdIoKernelConnector CreateConnector()
    {
        var pocketLoggerPath = Environment.GetEnvironmentVariable("POCKETLOGGER_LOG_PATH");
        string loggingArgs = null;

        if (File.Exists(pocketLoggerPath))
        {
            var logDir = Path.GetDirectoryName(pocketLoggerPath);
            loggingArgs = $"--verbose --log-path {logDir}";
        }

        var dotnetInteractive = typeof(Program).Assembly.Location;
        var hostUri = KernelHost.CreateHostUri("VS");
        var connector = new StdIoKernelConnector(
            new[] { "dotnet", $""" "{dotnetInteractive}" stdio {loggingArgs}""" },
            hostUri);
        return connector;
    }

    [Fact]
    public void when_all_created_proxies_have_been_disposed_then_the_remote_process_is_killed()
    {
     
        


        // TODO (when______then_remote_process_is_killed) write test
        throw new NotImplementedException();
    }

    [Fact]
    public async Task it_can_return_a_proxy_to_a_remote_composite()
    {
        var connector = CreateConnector();
        
        var kernel = await connector.CreateKernelAsync("Proxy");

        kernel.Should().BeEquivalentTo(new { x = 123 });

        using var _ = new AssertionScope();

        kernel.KernelInfo.IsProxy.Should().BeTrue();
        kernel.KernelInfo.IsComposite.Should().BeTrue();
    }

    [Fact]
    public void it_can_create_a_proxy_kernel_with_a_different_name_than_the_remote()
    {
        




        // TODO (it_can_create_a_proxy_kernel_with_a_differnet_local_name_than_its_remote_name) write test
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