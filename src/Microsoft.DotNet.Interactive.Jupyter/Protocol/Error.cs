// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.Error)]
    public class Error : PubSubMessage
    {
        [JsonPropertyName("ename")]
        public string EName { get;  }

        [JsonPropertyName("evalue")]
        public string EValue { get;  }

        [JsonPropertyName("traceback")]
        public IReadOnlyList<string> Traceback { get; } 

        public Error(string eName, string eValue, IReadOnlyList<string> traceback = null)
        {
            EName = eName;
            EValue = eValue;
            Traceback = traceback ?? new List<string>();
        }
    }
}