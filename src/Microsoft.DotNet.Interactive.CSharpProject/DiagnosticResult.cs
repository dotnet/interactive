// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class DiagnosticResult
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; }

        
        public IReadOnlyCollection<SerializableDiagnostic> Diagnostics { get; set; }

        public DiagnosticResult(IReadOnlyCollection<SerializableDiagnostic> diagnostics = null, string requestId = null)
        {
            Diagnostics = diagnostics ?? Array.Empty<SerializableDiagnostic>();
            RequestId = requestId;
        }
    }
}
