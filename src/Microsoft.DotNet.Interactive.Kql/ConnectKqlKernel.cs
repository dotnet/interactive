// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Kql;

public class ConnectKqlKernel : ConnectKernelCommand
{
    public ConnectKqlKernel(string connectedKernelName) : base(connectedKernelName)
    {
    }

    public string Cluster { get; set; }
    public string Database { get; set; }
}