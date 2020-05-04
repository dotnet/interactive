// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JsonConverter(typeof(InputOutputHistoryElementConverter))]
    public class InputOutputHistoryElement : InputHistoryElement
    {
        public string Output { get; }
        public InputOutputHistoryElement(int session, int lineNumber, string input, string output) : base(session, lineNumber, input)
        {
            Output = output;
        }
    }
}