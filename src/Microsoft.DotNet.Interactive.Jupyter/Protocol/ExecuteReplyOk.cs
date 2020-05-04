// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ExecuteReply)]
    public class ExecuteReplyOk : ExecuteReply
    {
        public ExecuteReplyOk(IReadOnlyList<IReadOnlyDictionary<string, string>> payload = null, IReadOnlyDictionary<string, string> userExpressions = null , int executionCount = 0): base(status: StatusValues.Ok, executionCount: executionCount)
        {
            UserExpressions = userExpressions ?? new Dictionary<string, string>();
            Payload = payload ?? new List<IReadOnlyDictionary<string, string>>();
        }

        [JsonProperty("payload")]
        public IReadOnlyList<IReadOnlyDictionary<string,string>> Payload { get;  }

        [JsonProperty("user_expressions")]
        public IReadOnlyDictionary<string,string> UserExpressions { get; }
    }
}
