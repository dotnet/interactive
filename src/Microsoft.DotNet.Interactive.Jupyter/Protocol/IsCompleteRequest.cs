// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.IsCompleteRequest)]
    public class IsCompleteRequest : RequestMessage
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        public IsCompleteRequest(string code)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }
    }
}