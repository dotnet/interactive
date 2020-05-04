// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.InspectRequest)]
    public class InspectRequest : RequestMessage
    {
        [JsonProperty("code")]
        public string Code { get; }

        [JsonProperty("cursor_pos")]
        public int CursorPos { get; }

        [JsonProperty("detail_level")]
        public int DetailLevel { get; }

        public InspectRequest(string code, int cursorPos, int detailLevel)
        {
            Code = code;
            CursorPos = cursorPos;
            DetailLevel = detailLevel;
        }
    }
}