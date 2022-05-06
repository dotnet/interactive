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
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests;

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
            _configuration.WorkingDirectory);

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

        var res = await localCompositeKernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName", "newKernelName"));
        
        var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

        var events = res.KernelEvents.ToSubscribedList();

        events
            .Should()
            .EventuallyContainSingle<DisplayEvent>(
                @where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected),
                timeout: 10_000);
    }

    protected override SubmitCode CreateConnectCommand(string localKernelName)
    {
        return new SubmitCode(
            $"#!connect stdio --kernel-name {localKernelName} --command \"{_configuration.Command}\" {string.Join(" ", _configuration.Args)}");
    }

    protected override void AddKernelConnector(CompositeKernel compositeKernel)
    {
        compositeKernel.AddKernelConnector(new ConnectStdIoCommand());
    }
}

public class StdioConnectionTestConfiguration
{
    public string Command { get; set; }

    public string[] Args { get; set; }
    
    public DirectoryInfo WorkingDirectory { get; set; }
}