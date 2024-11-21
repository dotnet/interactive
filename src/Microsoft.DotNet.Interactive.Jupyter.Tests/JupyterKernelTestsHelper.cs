// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterConnectionTestData
{
    private SimulatedJupyterConnectionOptions _connectionOptions;

    public JupyterConnectionTestData(
        string connectionType,
        SimulatedJupyterConnectionOptions connectionOptions = null,
        string connectionString = "")
    {
        ConnectionType = connectionType;
        ConnectionString = connectionString;
        _connectionOptions = connectionOptions;
    }

    public string ConnectionType { get; }

    public string KernelSpecName { get; set; }

    public string ConnectionString { get; }

    public SimulatedJupyterConnectionOptions GetConnectionOptions(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string fileName = "") =>
        _connectionOptions ??= new SimulatedJupyterConnectionOptions(KernelSpecName, filePath, fileName);
}