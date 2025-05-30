// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Http;
using System;
using System.CommandLine;
using System.IO;
using System.Net.NetworkInformation;

namespace Microsoft.DotNet.Interactive.App.CommandLine;

public class StartupOptions
{
    public StartupOptions(
        DirectoryInfo logPath = null,
        bool verbose = false,
        HttpPortRange httpPortRange = null,
        HttpPort httpPort = null,
        Uri kernelHostUri = null,
        DirectoryInfo workingDir = null,
        bool httpLocalOnly = false,
        FileInfo jupyterConnectionFile = null,
        string defaultKernel = null)
    {
        LogPath = logPath;
        Verbose = verbose;
        HttpPortRange = httpPortRange;
        HttpPort = httpPort;
        KernelHostUri = kernelHostUri;
        WorkingDir = workingDir;
        JupyterConnectionFile = jupyterConnectionFile;
        DefaultKernel = defaultKernel;

        if (httpLocalOnly)
        {
            GetAllNetworkInterfaces = GetNetworkInterfacesHttpLocalOnly;
        }
        else
        {
            GetAllNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces;
        }
    }

    public DirectoryInfo LogPath { get; }

    public bool Verbose { get; }

    public HttpPort HttpPort { get; set; }

    public HttpPortRange HttpPortRange { get; }

    public Uri KernelHostUri { get; }

    public DirectoryInfo WorkingDir { get; }

    public FileInfo JupyterConnectionFile { get; }

    public string DefaultKernel { get; }

    public Func<NetworkInterface[]> GetAllNetworkInterfaces { get; }

    public bool EnableHttpApi => HttpPort is not null || HttpPortRange is not null;

    public static NetworkInterface[] GetNetworkInterfacesHttpLocalOnly()
    {
        return [];
    }

    public static StartupOptions Parse(ParseResult parseResult) =>
        new(parseResult.GetValue<DirectoryInfo>("--log-path"),
            parseResult.GetValue<bool>("--verbose"),
            parseResult.GetValue<HttpPortRange>("--http-port-range"),
            parseResult.GetValue<HttpPort>("--http-port"),
            parseResult.GetValue<Uri>("--kernel-host"),
            parseResult.GetValue<DirectoryInfo>("--working-dir"),
            parseResult.GetValue<bool>("--http-local-only"),
            parseResult.GetValue<FileInfo>("connection-file"),
            parseResult.GetValue<string>("--default-kernel"));
}