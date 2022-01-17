// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public class StdioConnectionTests : KernelConnectionTestsBase<StdioConnectionTestConfiguration>
{
    public StdioConnectionTests(ITestOutputHelper output) : base(output)
    {
    }


    protected override Task<IKernelConnector> CreateConnectorAsync(StdioConnectionTestConfiguration configuration)
    {
        var command = new List<string>{configuration.Command};
        if (configuration.Args?.Length > 0)
        {
            command.AddRange(configuration.Args);
        }

        var connector = new StdIoKernelConnector(command.ToArray(), configuration.WorkingDirectory);

        RegisterForDisposal(connector);

        return Task.FromResult<IKernelConnector>(connector);
    }

    protected override StdioConnectionTestConfiguration CreateConnectionConfiguration()
    {
        var toolPath = CopyToTemp(typeof(App.Program).Assembly.Location);
        
        return new StdioConnectionTestConfiguration
        {
            Command = Dotnet.Path.FullName,
            Args = new []
            {
                $"\"{toolPath.FullName}\"",
                "stdio",
                "--default-kernel",
                "csharp",
            },
            WorkingDirectory = toolPath.Directory
        };
    }

    private FileInfo CopyToTemp(string assemblyLocation)
    {
        var toolFileInfo = new FileInfo(assemblyLocation);

        var srcDir = toolFileInfo.Directory;
        var temporaryDirectory = TemporaryDirectory.CreateFromDeepCopy(srcDir);
        
        RegisterForDisposal(temporaryDirectory);

        return new FileInfo(Path.Combine(temporaryDirectory.Directory.FullName, toolFileInfo.Name));
    }

    [Fact]
    public async Task stdio_server_encoding_is_utf_8()
    {
        var configuration = CreateConnectionConfiguration();

        await CreateRemoteKernelTopologyAsync(configuration);

        using var localCompositeKernel = new CompositeKernel
        {
            new FakeKernel("fsharp")
        };

        ConfigureConnectCommand(localCompositeKernel);

        localCompositeKernel.DefaultKernelName = "fsharp";

        var connectToRemoteKernel = CreateConnectionCommand(configuration);

        await localCompositeKernel.SendAsync(connectToRemoteKernel);

        var res = await localCompositeKernel.SendAsync(new SubmitCode("System.Console.InputEncoding.EncodingName + \"/\" + System.Console.OutputEncoding.EncodingName", "newKernelName"));
        var expected = Encoding.UTF8.EncodingName + "/" + Encoding.UTF8.EncodingName;

        var events = res.KernelEvents.ToSubscribedList();

        events
            .Should()
            .EventuallyContainSingle<DisplayEvent>(
                where: d => d.FormattedValues.Any(FormattedValue => FormattedValue.Value == expected),
                timeout: 10_000);
    }

    protected override SubmitCode CreateConnectionCommand(StdioConnectionTestConfiguration configuration)
    {
        return new SubmitCode(
            $"#!connect stdio --kernel-name newKernelName --command \"{configuration.Command}\" {string.Join(" ", configuration.Args)}");
    }

    protected override void ConfigureConnectCommand(CompositeKernel compositeKernel)
    {
        compositeKernel.UseKernelClientConnection(new ConnectStdIoCommand());
    }

    protected override Task<IDisposable> CreateRemoteKernelTopologyAsync(StdioConnectionTestConfiguration configuration)
    {
        return Task.FromResult( Disposable.Empty);
    }

    protected override Task<IDisposable> ConnectHostAsync(CompositeKernel remoteKernel, StdioConnectionTestConfiguration configuration)
    {
        return Task.FromResult(Disposable.Empty);
    }
}

public class StdioConnectionTestConfiguration
{
    public string Command { get; set; }
    public string[] Args { get; set; }
    
    public DirectoryInfo WorkingDirectory { get; set; }
}