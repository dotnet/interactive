// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.CompleteReply)]
    public class CompleteReply : ReplyMessage
    {
        [JsonProperty("matches")]
        public IReadOnlyList<string> Matches { get; }

        [JsonProperty("cursor_start")]
        public int CursorStart { get; }

        [JsonProperty("cursor_end")]
        public int CursorEnd { get; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonProperty("status")] public string Status { get; }

        public CompleteReply(int cursorStart = 0, int cursorEnd = 0, IReadOnlyList<string> matches = null, IReadOnlyDictionary<string, object> metaData = null, string status = null)
        {
            CursorStart = cursorStart;
            CursorEnd = cursorEnd;
            Matches = matches ?? new List<string>();
            MetaData = metaData ?? new Dictionary<string, object>();
            Status = status ?? "ok";
        }
    }
}
