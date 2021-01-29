// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.ExecuteRequest)]
    public class ExecuteRequest : RequestMessage
    {
        [JsonPropertyName("code")]
        public string Code { get; }

        [JsonPropertyName("silent")]
        public bool Silent { get; }

        [JsonPropertyName("store_history")]
        public bool StoreHistory { get; }

        [JsonPropertyName("user_expressions")]
        public IReadOnlyDictionary<string, string> UserExpressions { get; }

        [JsonPropertyName("allow_stdin")]
        public bool AllowStdin { get; }

        [JsonPropertyName("stop_on_error")]
        public bool StopOnError { get; }

        public ExecuteRequest(string code, bool silent = false, bool storeHistory = false, bool allowStdin = true, bool stopOnError = false, IReadOnlyDictionary<string, string> userExpressions = null)
        {
            Silent = silent;
            StoreHistory = storeHistory;
            AllowStdin = allowStdin;
            StopOnError = stopOnError;
            UserExpressions = userExpressions ?? new Dictionary<string, string>();
            Code = code ?? string.Empty;
        }
    }
}
