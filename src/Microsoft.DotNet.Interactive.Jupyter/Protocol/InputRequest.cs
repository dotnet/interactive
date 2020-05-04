// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.InputRequest)]
    public class InputRequest : RequestMessage
    {
        [JsonProperty("prompt")]
        public string Prompt { get; }
        [JsonProperty("password")]
        public bool Password { get; set; }

        public InputRequest(string prompt = null, bool password = false)
        {
            Prompt = prompt;
            Password = password;
        }
    }
}