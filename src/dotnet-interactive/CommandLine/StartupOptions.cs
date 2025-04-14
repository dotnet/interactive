// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.DotNet.Interactive.Http;

namespace Microsoft.DotNet.Interactive.App.CommandLine;

public class StartupOptions
{
    public StartupOptions(
        DirectoryInfo logPath = null,
        bool verbose = false,
        HttpPortRange httpPortRange = null,
        HttpPort httpPort = null,
        Uri kernelHost = null,
        DirectoryInfo workingDir = null,
        bool httpLocalOnly = false
    )
    {
        LogPath = logPath;
        Verbose = verbose;
        HttpPortRange = httpPortRange;
        HttpPort = httpPort;
        KernelHost = kernelHost;
        WorkingDir = workingDir;

        if (httpLocalOnly)
            GetAllNetworkInterfaces = GetNetworkInterfacesHttpLocalOnly;
        else
            GetAllNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces;
    }

    public DirectoryInfo LogPath { get; }

    public bool Verbose { get; }

    public HttpPort HttpPort { get; internal set; }

    public HttpPortRange HttpPortRange { get; internal set; }
        
    public Uri KernelHost { get; }

    public DirectoryInfo WorkingDir { get; internal set; }

    public Func<NetworkInterface[]> GetAllNetworkInterfaces { get; }

    public bool EnableHttpApi => HttpPort is not null || HttpPortRange is not null;

    public static NetworkInterface[] GetNetworkInterfacesHttpLocalOnly()
    { 
        return [];
    }
}
