// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.CommOpen)]
    public class CommOpen : Message
    {
        [JsonPropertyName("comm_id")]
        public string CommId { get; set; }

        [JsonPropertyName("target_name")]
        public string TargetName { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Data { get; } = new Dictionary<string,object>();


    }
}