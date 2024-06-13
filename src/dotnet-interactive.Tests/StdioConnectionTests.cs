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
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class StdioConnectionTests : ProxyKernelConnectionTestsBase
{
    private readonly StdioConnectionTestConfiguration _configuration;

    public StdioConnectionTests(ITestOutputHelper output) : base(output)
    {
        _configuration = CreateConnectionConfiguration();
    }

    protected override Func<string, Task<ProxyKernel>> CreateConnector()
    {
        var command = new List<string> { _configuration.Command };

        if (_configuration.Args?.Length > 0)
        {
            command.AddRange(_configuration.Args);
        }

        return _ => new StdIoKernelConnector(
            command.ToArray(),
            rootProxyKernelLocalName: "rootProxy",
            kernelHostUri: new Uri("kernel://test-kernel"),
            workingDirectory: _configuration.WorkingDirectory).CreateRootProxyKernelAsync();
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

        var result = await localCompositeKernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName",
                                                                         "newKernelName"));

        var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

        result.Events
              .Should()
              .ContainSingle<DisplayEvent>(
                  where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected));
    }

    protected override SubmitCode CreateConnectCommand(string localKernelName)
    {
        return new SubmitCode(
            $"#!connect stdio --kernel-name {localKernelName} --command \"{_configuration.Command}\" {string.Join(" ", _configuration.Args)}");
    }

    protected override void AddKernelConnector(CompositeKernel compositeKernel)
    {
        compositeKernel.AddKernelConnector(new ConnectStdIoDirective(new Uri("kernel://test-kernel")));
    }
}

public class StdioConnectionTestConfiguration
{
    public string Command { get; set; }

    public string[] Args { get; set; }

    public DirectoryInfo WorkingDirectory { get; set; }
}