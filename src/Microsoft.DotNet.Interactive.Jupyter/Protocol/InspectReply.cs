// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.InspectReply)]
    public class InspectReply : ReplyMessage
    {
        public static InspectReply Ok(string source, IReadOnlyDictionary<string, object> data, IReadOnlyDictionary<string, object> metaData) => new InspectReply(source, StatusValues.Ok, data, metaData);
        public static InspectReply Error(string source, IReadOnlyDictionary<string, object> data, IReadOnlyDictionary<string, object> metaData) => new InspectReply(source, StatusValues.Error, data, metaData);
        public InspectReply(string source, string status, IReadOnlyDictionary<string, object> data = null, IReadOnlyDictionary<string, object> metaData = null)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(status));
            }
            Status = status;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Data = data?? new Dictionary<string, object>();
            MetaData = metaData?? new Dictionary<string, object>();
        }

        [JsonPropertyName("status")]
        public string Status { get; }

        [JsonPropertyName("source")]
        public string Source { get;  }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string,object> Data { get; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> MetaData { get; }
    }
}