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
            Contents = "";
        }

        public InteractiveDocumentElement(
            string? contents = null,
            string? language = null,
            IEnumerable<InteractiveDocumentOutputElement>? outputs = null)
        {
            Contents = contents ?? "";
            Language = language;
            Outputs = outputs is { }
                          ? new(outputs)
                          : new();
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        public string? Language { get; set; }

        public string Contents { get; set; }

        public List<InteractiveDocumentOutputElement> Outputs { get; } = new();

        public int ExecutionOrder { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, object>? Metadata { get; set; }

        internal string? InferredTargetKernelName { get; set; }
    }
}