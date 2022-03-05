// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class RequestDescriptors
    {
        [JsonProperty("_self")]
        public RequestDescriptor Self { get; }
        [JsonProperty("configuration")]
        public RequestDescriptor Configuration { get; set; }
        [JsonProperty("completion")]
        public RequestDescriptor Completion { get; set; }
        [JsonProperty("acceptCompletion")]
        public RequestDescriptor AcceptCompletion { get; set; }
        [JsonProperty("loadFromGist")]
        public RequestDescriptor LoadFromGist { get; set; }
        [JsonProperty("diagnostics")]
        public RequestDescriptor Diagnostics { get; set; }
        [JsonProperty("signatureHelp")]
        public RequestDescriptor SignatureHelp { get; set; }
        [JsonProperty("run")]
        public RequestDescriptor Run { get; set; }
        [JsonProperty("snippet")]
        public RequestDescriptor Snippet { get; set; }
        [JsonProperty("version")]
        public RequestDescriptor Version { get; set; }
        [JsonProperty("compile")]
        public RequestDescriptor Compile { get; set; }
        [JsonProperty("projectFromGist")]
        public RequestDescriptor ProjectFromGist { get; set; }
        [JsonProperty("regionsFromFiles")]
        public RequestDescriptor RegionsFromFiles { get; set; }
        [JsonProperty("getPackage")]
        public RequestDescriptor GetPackage { get; set; }


        public RequestDescriptors(RequestDescriptor self)
        {
            Self = self;
        }
    }
}