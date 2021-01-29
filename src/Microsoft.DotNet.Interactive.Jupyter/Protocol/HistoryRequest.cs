// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.HistoryRequest)]
    public class HistoryRequest : RequestMessage
    {
        [JsonPropertyName("output")]
        public bool Output { get; }

        [JsonPropertyName("hist_access_type")]
        public string AccessType { get; }

        [JsonPropertyName("raw")]
        public bool Raw { get; }

        [JsonPropertyName("session")]
        public int Session { get; }

        [JsonPropertyName("start")]
        public int Start { get; }

        [JsonPropertyName("stop")]
        public int Stop { get; }

        [JsonPropertyName("n")]
        public int N { get;}

        [JsonPropertyName("pattern")]
        public string Pattern { get;  }

        [JsonPropertyName("unique")]
        public bool Unique { get;  }

        public HistoryRequest(int session, string accessType = "range", int start = 0, int stop = 0, int n = 0,string pattern = null, bool unique = false, bool raw = false, bool output = false)
        {
            Session = session;
            AccessType = accessType;
            Start = start;
            Stop = stop;
            N = n;
            Unique = unique;
            Raw = raw;
            Output = output;
            Pattern = pattern?? string.Empty;
        }
    }
}