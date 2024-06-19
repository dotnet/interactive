// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

/// <summary>
/// Defines a magic command that can be used to connect a subkernel dynamically.
/// </summary>
public abstract class ConnectKernelCommand : KernelCommand
{
    protected ConnectKernelCommand(string connectedKernelName)
    {
        ConnectedKernelName = connectedKernelName;
    }

    [JsonInclude]
    [JsonPropertyName("kernelName")]
    public string ConnectedKernelName { get; private set; }
}