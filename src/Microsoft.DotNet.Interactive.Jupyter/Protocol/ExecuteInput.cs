// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ExecuteInput)]
    public class ExecuteInput : PubSubMessage
    {
        [JsonPropertyName("code")]
        public string Code { get; }

        [JsonPropertyName("execution_count")]
        public int ExecutionCount { get; }

        public ExecuteInput(string code = null, int executionCount = 0)
        {
            Code = code;
            ExecutionCount = executionCount;
        }
    }
}
