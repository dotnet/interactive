// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public abstract class HistoryElement
    {
        [JsonPropertyName("session")]
        public int Session { get; }

        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; }

        protected HistoryElement(int session, int lineNumber)
        {
            Session = session;
            LineNumber = lineNumber;
        }
    }
}