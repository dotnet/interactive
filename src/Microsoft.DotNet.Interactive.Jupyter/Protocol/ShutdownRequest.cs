// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.KernelShutdownRequest)]
    public class ShutdownRequest : RequestMessage
    {
        [JsonPropertyName("restart")]
        public bool Restart { get; }

        public ShutdownRequest(bool restart = false)
        {
            Restart = restart;
        }
    }
}