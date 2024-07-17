// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class ConnectJupyterKernel : ConnectKernelCommand
{
    public ConnectJupyterKernel(string connectedKernelName) : base(connectedKernelName)
    {
    }

    public string CondaEnv { get; set; }

    [JsonPropertyName("kernelSpec")]
    public string KernelSpecName { get; set; }

    public string InitScript { get; set; }

    public string TargetUrl { get; set; }
    
    public bool UseBearerAuth { get; set; }
    
    public string Token { get; set; }
}