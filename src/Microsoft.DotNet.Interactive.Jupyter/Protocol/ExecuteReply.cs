// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ExecuteReply)]
    public class ExecuteReply : ReplyMessage
    {
        [JsonPropertyName("status")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Status { get; }

        [JsonPropertyName("execution_count")]
        public int ExecutionCount { get; }

        public ExecuteReply(string status = null, int executionCount = 0)
        {
            Status = status;
            ExecutionCount = executionCount;
        }

    }
}
