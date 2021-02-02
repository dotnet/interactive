// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JsonConverter(typeof(HistoryReplyConverter))]
    [JupyterMessageType(JupyterMessageContentTypes.HistoryReply)]
    public class HistoryReply : ReplyMessage
    {
        [JsonPropertyName("history")]
        public IReadOnlyList<HistoryElement> History { get; }

        public HistoryReply(IReadOnlyList<HistoryElement> history = null)
        {
            History = history ?? new List<HistoryElement>();
        }
    }
}