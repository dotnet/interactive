// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ClearOutput)]
    public class ClearOutput : PubSubMessage
    {
        [JsonProperty("wait ")]
        public bool Wait { get; }

        public ClearOutput(bool wait = false)
        {
            Wait = wait;
        }
    }
}