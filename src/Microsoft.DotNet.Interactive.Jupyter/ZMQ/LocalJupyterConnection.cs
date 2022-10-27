// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

internal class LocalJupyterConnection : IJupyterConnection
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IJupyterKernelSpecModule _kernelSpecModule;
    private Process _kernelProcess;

    public LocalJupyterConnection(IJupyterKernelSpecModule kernelSpecModule = null)
    {
        _kernelSpecModule = kernelSpecModule ?? new JupyterKernelSpecModule();
    }

    public Task<IReadOnlyCollection<string>> GetKernelSpecNamesAsync()
    {
        var specsDirs = _kernelSpecModule.GetInstalledKernelDirectories();
        return Task.FromResult<IReadOnlyCollection<string>>(specsDirs?.Keys.ToArray());
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelType)
    {
        var connectionInfo = await LaunchKernel(kernelType);

        if (connectionInfo != null && !_kernelProcess.HasExited) {
            var kernelConnection = new ZMQKernelConnection(connectionInfo, _kernelProcess.Id);
            return kernelConnection;
        }

        throw new KernelLaunchException(kernelType, $"Process Exited with exit code {_kernelProcess.ExitCode}");
    }

    private async Task<ConnectionInformation> LaunchKernel(string kernelType)
    {
        // find the related kernel spec for the kernel type 
        var spec = GetKernelSpec(kernelType);

        if (spec is null)
        {
            throw new KernelLaunchException(kernelType, "kernel not found");
        }

        ConnectionInformation connectionInfo = null;
        // create a connection file with available ports in jupyer runtime

        // avoid potential port race conditions by reserving ports up front 
        // and releasing before kernel launch.
        List<string> kernelArgs = null;
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

            kernelArgs = new List<string>(spec.CommandArguments);
            kernelArgs.Remove("{connection_file}");
            kernelArgs.Add($"\"{runtimeConnectionFile}\"");
        }

        if (kernelArgs != null)
        {
            // use the process info in kernel spec and replace the {connection file} in the data with file path
            // spawn a child process. This will create ZMQ sockets on the kernel. 
            var command = kernelArgs[0];
            var arguments = string.Join(" ", kernelArgs.Skip(1));

            _kernelProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            await Task.Yield();

            _kernelProcess.Start();
            _disposables.Add(_kernelProcess);
        }

        // now pass the connection file to kernel connection and bind to the sockets. 
        return connectionInfo;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private KernelSpec GetKernelSpec(string kernelType)
    {
        var installedSpecs = _kernelSpecModule.GetInstalledKernelDirectories(); 

        if (installedSpecs.ContainsKey(kernelType))
        {
            var directory = installedSpecs[kernelType];
            var kernelJsonPath = Path.Combine(directory.FullName, "kernel.json");
            if (File.Exists(kernelJsonPath))
            {
                var kernelJson = JsonDocument.Parse(File.ReadAllText(kernelJsonPath));
                var spec = JsonSerializer.Deserialize<KernelSpec>(kernelJson, JsonFormatter.SerializerOptions);
                return spec;
            }
        }

        return null;
    }
}
