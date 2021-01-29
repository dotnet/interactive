// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ExecuteResult)]
    public class ExecuteResult : PubSubMessage
    {
        [JsonPropertyName("source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Source { get; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> Data { get; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonPropertyName("transient")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> Transient { get; }

        [JsonPropertyName("execution_count")]
        public int ExecutionCount { get; }

        public ExecuteResult(int executionCount, string source = null, IReadOnlyDictionary<string, object> data = null, IReadOnlyDictionary<string, object> metaData = null, IReadOnlyDictionary<string, object> transient = null)
        {
            Source = source;
            Data = data ?? new Dictionary<string, object>();
            Transient = transient ?? new Dictionary<string, object>();
            MetaData = metaData ?? new Dictionary<string, object>();
            ExecutionCount = executionCount;
        }
    }
}