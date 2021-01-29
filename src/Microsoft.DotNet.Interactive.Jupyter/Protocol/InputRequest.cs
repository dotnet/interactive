// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.InputRequest)]
    public class InputRequest : RequestMessage
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; }
        [JsonPropertyName("password")]
        public bool Password { get; set; }

        public InputRequest(string prompt = null, bool password = false)
        {
            Prompt = prompt;
            Password = password;
        }
    }
}