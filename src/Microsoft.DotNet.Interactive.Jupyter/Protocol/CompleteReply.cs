// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol;

[JupyterMessageType(JupyterMessageContentTypes.CompleteReply)]
public class CompleteReply : ReplyMessage
{
    [JsonPropertyName("matches")]
    public IReadOnlyList<string> Matches { get; }

    [JsonPropertyName("cursor_start")]
    public int CursorStart { get; }

    [JsonPropertyName("cursor_end")]
    public int CursorEnd { get; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<CompletionResultMetadata>> MetaData { get; }

    [JsonPropertyName("status")] 
    public string Status { get; }

    public CompleteReply(int cursorStart = 0, int cursorEnd = 0, IReadOnlyList<string> matches = null, IReadOnlyDictionary<string, IReadOnlyList<CompletionResultMetadata>> metaData = null, string status = null)
    {
        CursorStart = cursorStart;
        CursorEnd = cursorEnd;
        Matches = matches ?? new List<string>();
        MetaData = metaData ?? new Dictionary<string, IReadOnlyList<CompletionResultMetadata>>();
        Status = status ?? "ok";
    }
}
