// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class ClientConfiguration
    {
        [JsonProperty("versionId")]
        public string VersionId { get; }

        [JsonProperty("defaultTimeoutMs")]
        public int DefaultTimeoutMs { get; }

        [JsonProperty("_links")]
        public RequestDescriptors Links { get; }

        [JsonProperty("applicationInsightsKey")]
        public string ApplicationInsightsKey { get; }

        [JsonProperty("enableBranding")]
        public bool EnableBranding { get; }

        public ClientConfiguration(string versionId,
                                   RequestDescriptors links,
                                   int defaultTimeoutMs,
                                   string applicationInsightsKey,
                                   bool enableBranding)
        {
            if (defaultTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(defaultTimeoutMs));
            if (string.IsNullOrWhiteSpace(versionId))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(versionId));
            VersionId = versionId;
            Links = links ?? throw new ArgumentNullException(nameof(links));
            DefaultTimeoutMs = defaultTimeoutMs;
            ApplicationInsightsKey = applicationInsightsKey ?? throw new ArgumentNullException(nameof(applicationInsightsKey));
            EnableBranding = enableBranding;
        }
    }
}