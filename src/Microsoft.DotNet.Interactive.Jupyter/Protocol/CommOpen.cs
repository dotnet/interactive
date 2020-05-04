// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.CommOpen)]
    public class CommOpen : Message
    {
        [JsonProperty("comm_id")]
        public string CommId { get; set; }

        [JsonProperty("target_name")]
        public string TargetName { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; } = new JObject();


    }
}