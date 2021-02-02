// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.CompleteRequest)]
    public class CompleteRequest : RequestMessage
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("cursor_pos")]
        public int CursorPosition { get; set; }

        public CompleteRequest(string code, int cursorPosition = 0)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(code));
            }
            Code = code;
            CursorPosition = cursorPosition;
        }
    }
}
