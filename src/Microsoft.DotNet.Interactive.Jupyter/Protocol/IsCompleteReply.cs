// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.IsCompleteReply)]
    public class IsCompleteReply : ReplyMessage
    {
        //One of 'complete', 'incomplete', 'invalid', 'unknown'
        [JsonProperty("status")]
        public string Status { get; }

        //If status is 'incomplete', indent should contain the characters to use
        //to indent the next line. This is only a hint: frontends may ignore it
        // and use their own autoindentation rules. For other statuses, this
        // field does not exist.
        [JsonProperty("indent")]
        public string Indent { get; }

        public IsCompleteReply(string indent, string status)
        {
            Indent = indent;
            Status = status;
        }
    }
}