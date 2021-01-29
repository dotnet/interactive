// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class Link
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}