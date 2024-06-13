// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class ConnectStdio : ConnectKernelCommand
{
    public ConnectStdio(string connectedKernelName) : base(connectedKernelName)
    {
    }

    public string[] Command { get; set; }

    public string KernelHostUri { get; set; }

    public DirectoryInfo WorkingDirectory { get; set; }
}