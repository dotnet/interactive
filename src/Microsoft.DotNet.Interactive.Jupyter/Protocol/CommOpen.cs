// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol;

[JupyterMessageType(JupyterMessageContentTypes.CommOpen)]
public class CommOpen : Message
{
    [JsonPropertyName("comm_id")]
    public string CommId { get; }

    [JsonPropertyName("target_name")]
    public string TargetName { get; }

    [JsonPropertyName("data")]
    public IReadOnlyDictionary<string, object> Data { get; }

    public CommOpen(string commId, string targetName, IReadOnlyDictionary<string, object> data)
    {
        if (string.IsNullOrWhiteSpace(commId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(commId));
        }

        if (string.IsNullOrWhiteSpace(targetName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetName));
        }

        CommId = commId;
        TargetName = targetName;
        Data = data ?? new Dictionary<string, object>();
    }
}