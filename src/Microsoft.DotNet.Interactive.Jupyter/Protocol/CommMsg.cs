// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.CommMsg)]
    public class CommMsg : Message
    {
        [JsonPropertyName("comm_id")]
        public string CommId { get; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> Data { get; }

        public CommMsg(string commId, IReadOnlyDictionary<string, object> data = null)
        {
            if (string.IsNullOrWhiteSpace(commId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(commId));
            }
            CommId = commId;
            Data = data ?? new Dictionary<string, object>();
        }
    }
}