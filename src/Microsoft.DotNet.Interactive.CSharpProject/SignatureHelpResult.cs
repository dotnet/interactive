// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class SignatureHelpResult
    {
        private IEnumerable<SignatureHelpItem> signatures ;

        public IEnumerable<SignatureHelpItem> Signatures
        {
            get => signatures ?? (signatures = Array.Empty<SignatureHelpItem>());
            set => signatures  = value;
        }

        public int ActiveSignature { get; set; }

        public int ActiveParameter { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }
        public IEnumerable<SerializableDiagnostic> Diagnostics { get; set; }


        public SignatureHelpResult(IEnumerable<SignatureHelpItem> signatures = null, IEnumerable<SerializableDiagnostic> diagnostics = null, string requestId = null)
        {
            RequestId = requestId;
            Signatures = signatures;
            Diagnostics = diagnostics;
        }
    }
}