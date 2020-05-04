// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JsonConverter(typeof(InputHistoryElementConverter))]
    public class InputHistoryElement: HistoryElement
    {
        public string Input { get; }
        public InputHistoryElement(int session, int lineNumber, string input) : base(session, lineNumber)
        {
            Input = input;
        }
    }
}