// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Documents
{
    public class InteractiveDocumentElement
    {
        [JsonConstructor]
        public InteractiveDocumentElement()
        {
        }

        public InteractiveDocumentElement(
            string language,
            string contents,
            IEnumerable<InteractiveDocumentOutputElement>? outputs = null)
        {
            Contents = contents;
            Language = language;
            Outputs = outputs is { }
                          ? new(outputs)
                          : new();
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; set; }

        public string Language { get; set; }

        public string Contents { get; set; }

        public List<InteractiveDocumentOutputElement> Outputs { get; } = new();

        public int ExecutionCount { get; set; }

        public Dictionary<string, object> Metadata { get; } = new();
    }
}