// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.InputReply)]
    public class InputReply : ReplyMessage
    {
        [JsonPropertyName("value")]
        public string Value { get; }

        public InputReply(string value)
        {
            Value = value;
        }
    }
}