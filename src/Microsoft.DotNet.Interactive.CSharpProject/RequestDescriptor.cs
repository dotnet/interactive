// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class RequestDescriptor
{
    [JsonProperty("timeoutMs")]
    public int TimeoutMs { get; }
    [JsonProperty("href")]
    public string Href { get; }
    [JsonProperty("templated")]
    public bool Templated { get; }
    [JsonProperty("properties")]
    public IEnumerable<RequestDescriptorProperty> Properties { get; }
    [JsonProperty("method")]
    public string Method { get; }
    [JsonProperty("body")]
    public string Body { get; }

    public RequestDescriptor(string href, string method = null, bool templated = false, IReadOnlyCollection<RequestDescriptorProperty> properties = null, string requestBody = null, int timeoutMs = 15000)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(href));
        }
        TimeoutMs = timeoutMs;
        Href = href;
        Templated = templated;
        Properties = properties ?? Array.Empty<RequestDescriptorProperty>();
        Method = method ?? "POST";
        Body = requestBody;
    }
}