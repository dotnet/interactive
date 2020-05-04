// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ExecuteResult)]
    public class ExecuteResult : PubSubMessage
    {
        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> Data { get; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonProperty("transient", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> Transient { get; }

        [JsonProperty("execution_count", NullValueHandling = NullValueHandling.Ignore)]
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