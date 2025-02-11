// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class ConnectStdio : ConnectKernelCommand
{
    private DirectoryInfo _workingDirectory;

    public ConnectStdio(string connectedKernelName) : base(connectedKernelName)
    {
    }

    public string[] Command { get; set; }

    [JsonPropertyName("kernelHost")]
    public string KernelHostUri { get; set; }

    [JsonIgnore]
    public DirectoryInfo WorkingDirectory
    {
        get => _workingDirectory ??= new DirectoryInfo(Directory.GetCurrentDirectory());
        set => _workingDirectory = value;
    }
}