// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.HistoryRequest)]
    public class HistoryRequest : RequestMessage
    {
        [JsonProperty("output")]
        public bool Output { get; }

        [JsonProperty("hist_access_type")]
        public string AccessType { get; }

        [JsonProperty("raw")]
        public bool Raw { get; }

        [JsonProperty("session")]
        public int Session { get; }

        [JsonProperty("start")]
        public int Start { get; }

        [JsonProperty("stop")]
        public int Stop { get; }

        [JsonProperty("n")]
        public int N { get;}

        [JsonProperty("pattern")]
        public string Pattern { get;  }

        [JsonProperty("unique")]
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