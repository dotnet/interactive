// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol;

[JupyterMessageType(JupyterMessageContentTypes.InspectReply)]
public class InspectReply : ReplyMessage
{
    public static InspectReply Ok(bool found, IReadOnlyDictionary<string, object> data, IReadOnlyDictionary<string, object> metaData) => new InspectReply(StatusValues.Ok, found, data, metaData);
    public static InspectReply Error(IReadOnlyDictionary<string, object> data, IReadOnlyDictionary<string, object> metaData) => new InspectReply(StatusValues.Error, false, data, metaData);
    public InspectReply(string status, bool found, IReadOnlyDictionary<string, object> data = null, IReadOnlyDictionary<string, object> metaData = null)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(status));
        }
        Status = status;
        Found = found;
        Data = data?? new Dictionary<string, object>();
        MetaData = metaData?? new Dictionary<string, object>();
    }

    [JsonPropertyName("status")]
    public string Status { get; }

    [JsonPropertyName("found")]
    public bool Found { get;  }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string,object> Data { get; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object> MetaData { get; }
}