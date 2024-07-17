// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.NamedPipeConnector;

public class ConnectNamedPipe : ConnectKernelCommand
{
    public ConnectNamedPipe(string connectedKernelName, string pipeName) : base(connectedKernelName)
    {
        PipeName = pipeName;
    }

    public string PipeName { get; }
}