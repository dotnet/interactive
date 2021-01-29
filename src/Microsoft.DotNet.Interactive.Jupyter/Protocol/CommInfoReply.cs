// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.CommInfoReply)]
    public class CommInfoReply : ReplyMessage
    {
        [JsonPropertyName("comms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, CommTarget> Comms { get; }

        public CommInfoReply(IReadOnlyDictionary<string, CommTarget> comms)
        {
            Comms = comms;
        }
    }
}