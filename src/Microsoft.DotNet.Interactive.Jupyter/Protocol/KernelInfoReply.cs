// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.KernelInfoReply)]
    public class KernelInfoReply : ReplyMessage
    {
        [JsonProperty("protocol_version")]
        public string ProtocolVersion { get; }

        [JsonProperty("implementation")]
        public string Implementation { get; }

        [JsonProperty("implementation_version")]
        public string ImplementationVersion { get; }

        [JsonProperty("language_info", NullValueHandling = NullValueHandling.Ignore)]
        public LanguageInfo LanguageInfo { get; }

        [JsonProperty("banner")]
        public string Banner { get; }

        [JsonProperty("status")]
        public string Status { get; }

        [JsonProperty("help_links")]
        public IReadOnlyList<Link> HelpLinks { get; }

        public KernelInfoReply(string protocolVersion, string implementation, string implementationVersion, LanguageInfo languageInfo, string banner = null, IReadOnlyList<Link> helpLinks = null)
        {
            if (string.IsNullOrWhiteSpace(protocolVersion))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(protocolVersion));
            }

            if (string.IsNullOrWhiteSpace(implementation))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(implementation));
            }

            if (string.IsNullOrWhiteSpace(implementationVersion))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(implementationVersion));
            }

            ProtocolVersion = protocolVersion;
            Implementation = implementation;
            ImplementationVersion = implementationVersion;
            LanguageInfo = languageInfo ?? throw new ArgumentNullException(nameof(languageInfo));
            Banner = banner;
            HelpLinks = helpLinks ?? new List<Link>();
            Status = StatusValues.Ok;
        }
    }
}