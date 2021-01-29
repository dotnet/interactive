// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class CommTarget
    {
        [JsonPropertyName("target_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TargetName { get; set; }

        public CommTarget(string targetName)
        {
            TargetName = targetName;
        }
    }
}