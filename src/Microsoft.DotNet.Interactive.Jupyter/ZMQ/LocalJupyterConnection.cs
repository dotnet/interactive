// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

internal class LocalJupyterConnection : IJupyterConnection
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IJupyterKernelSpecModule _kernelSpecModule;

    public LocalJupyterConnection(IJupyterKernelSpecModule kernelSpecModule = null)
    {
        _kernelSpecModule = kernelSpecModule ?? new JupyterKernelSpecModule();
    }

    public async Task<IReadOnlyCollection<string>> GetKernelSpecNamesAsync()
    {
        var specs = await _kernelSpecModule.ListKernels();
        return specs?.Keys.ToArray();
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelType)
    {
        // find the related kernel spec for the kernel type 
        var spec = await GetKernelSpecAsync(kernelType);

        if (spec is null)
        {
            throw new KernelStartException(kernelType, "kernel not found");
        }

        ConnectionInformation connectionInfo = null;
        // create a connection file with available ports in jupyer runtime

        // avoid potential port race conditions by reserving ports up front 
        // and releasing before kernel launch.
        Process kernelProcess = null;
        using (var tcpPortReservation = TcpPortReservations.ReserveFreePorts(5))
        {
            var reservedPorts = tcpPortReservation.Ports;

            connectionInfo = new ConnectionInformation()
            {
                ShellPort = reservedPorts[0],
                IOPubPort = reservedPorts[1],
                StdinPort = reservedPorts[2],
                ControlPort = reservedPorts[3],
                HBPort = reservedPorts[4],
                IP = IPAddress.Loopback.ToString(), // "127.0.0.1",
                Key = Guid.NewGuid().ToString(),
                Transport = "tcp",
                SignatureScheme = "hmac-sha256"
            };

            var fileName = $"kernel-{Guid.NewGuid().ToString()}.json";
            var connectionInfoTempFilePath = Path.Combine(Path.GetTempPath(), fileName);
            using (var writer = new FileStream(connectionInfoTempFilePath, FileMode.CreateNew))
            {
                await JsonSerializer.SerializeAsync(writer, connectionInfo, connectionInfo.GetType(), JsonFormatter.SerializerOptions);
            }

            var runtimeConnectionFile = Path.Combine(JupyterCommonDirectories.GetRuntimeDirectory().FullName, fileName);
            File.Copy(connectionInfoTempFilePath, runtimeConnectionFile);
            File.Delete(connectionInfoTempFilePath);

            kernelProcess = CreateKernelProcess(spec, runtimeConnectionFile);
        }

        if (kernelProcess == null)
        {
            throw new KernelStartException(kernelType, "count not create process.");
        }
        await Task.Yield();

        kernelProcess.Start();

        if (connectionInfo == null || kernelProcess.HasExited)
        {
            throw new KernelStartException(kernelType, $"Process Exited with exit code {kernelProcess.ExitCode}. Ensure you are running in the correct environment.");
        }

        var kernelConnection = new ZMQKernelConnection(connectionInfo, kernelProcess);
        return kernelConnection;
    }

    private Process CreateKernelProcess(KernelSpec spec, string connectionFilePath)
    {
        List<string> kernelArgs = new(spec?.CommandArguments);

        if (kernelArgs.Count == 0)
        {
            return null;
        }
        kernelArgs.Remove("{connection_file}");
        kernelArgs.Add($"\"{connectionFilePath}\"");

        // use the process info in kernel spec and replace the {connection_file} in the data with file path
        // spawn a child process. This will create ZMQ sockets on the kernel. 
        var command = kernelArgs[0];
        var arguments = string.Join(" ", kernelArgs.Skip(1));

        var kernelProcess = CommandLine.StartProcess(command, arguments, null);
        return kernelProcess;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task<KernelSpec> GetKernelSpecAsync(string kernelType)
    {
        var installedSpecs = await _kernelSpecModule.ListKernels();
        if (installedSpecs.ContainsKey(kernelType))
        {
            return installedSpecs[kernelType];
        }

        return null;
    }
}
