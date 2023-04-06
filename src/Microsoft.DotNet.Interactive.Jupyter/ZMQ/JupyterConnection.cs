// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

internal class JupyterConnection : IJupyterConnection
{
    private readonly Task<IReadOnlyDictionary<string, KernelSpec>> _getKernelSpecs;

    public JupyterConnection(IJupyterKernelSpecModule kernelSpecModule)
    {
        if (kernelSpecModule is null)
        {
            throw new ArgumentNullException(nameof(kernelSpecModule));
        }

        _getKernelSpecs = Task.Run(() => kernelSpecModule.ListKernels());
    }

    public async Task<IEnumerable<KernelSpec>> GetKernelSpecsAsync()
    {
        var specs = await _getKernelSpecs;
        return specs?.Values;
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelSpecName)
    {
        // find the related kernel spec for the kernel type 
        var spec = await GetKernelSpecAsync(kernelSpecName);

        if (spec is null)
        {
            throw new ArgumentException($"KernelSpec {kernelSpecName} not found");
        }

        ConnectionInformation connectionInfo = null;
        // create a connection file with available ports in jupyer runtime

        // avoid potential port race conditions by reserving ports up front 
        // and releasing before kernel launch.
        string connectionFilePath = null;
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

            var directory = JupyterCommonDirectories.GetRuntimeDirectory();
            if (!directory.Exists)
            {
                directory.Create();
            }
            var runtimeConnectionFile = Path.Combine(directory.FullName, fileName);
            File.Copy(connectionInfoTempFilePath, runtimeConnectionFile);
            File.Delete(connectionInfoTempFilePath);

            connectionFilePath = runtimeConnectionFile;
        }

        var kernelProcess = CreateKernelProcess(spec, connectionFilePath);
        if (kernelProcess == null)
        {
            throw new KernelStartException(kernelSpecName, "count not create process.");
        }

        await Task.Yield();

        if (connectionInfo == null || kernelProcess.HasExited)
        {
            throw new KernelStartException(kernelSpecName, $"Process Exited with exit code {kernelProcess.ExitCode}. Ensure you are running in the correct environment.");
        }

        var kernelConnection = new ZMQKernelConnection(connectionInfo, kernelProcess, kernelSpecName);
        return kernelConnection;
    }

    private Process CreateKernelProcess(KernelSpec spec, string connectionFilePath)
    {
        List<string> kernelArgs = new(spec?.CommandArguments);

        if (kernelArgs.Count == 0)
        {
            return null;
        }

        // point to the connection file with port information
        var indexOfConnectionFile = kernelArgs.FindIndex(s => s == "{connection_file}");
        kernelArgs[indexOfConnectionFile] = $"\"{connectionFilePath}\"";

        // use the process info in kernel spec and replace the {connection_file} in the data with file path
        // spawn a child process. This will create ZMQ sockets on the kernel. 
        var command = kernelArgs[0];
        var arguments = string.Join(" ", kernelArgs.Skip(1));

        Logger kernelLog = new(spec.Name);
        var kernelProcess = CommandLine.StartProcess(command,
                                                     arguments,
                                                     null,
                                                     o => kernelLog.Info(o),
                                                     err => kernelLog.Error(err));
        return kernelProcess;
    }

    private async Task<KernelSpec> GetKernelSpecAsync(string kernelSpecName)
    {
        var installedSpecs = await _getKernelSpecs;
        if (installedSpecs.ContainsKey(kernelSpecName))
        {
            return installedSpecs[kernelSpecName];
        }

        return null;
    }
}
