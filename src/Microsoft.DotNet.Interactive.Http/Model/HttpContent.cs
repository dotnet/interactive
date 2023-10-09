// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Http;

public sealed class HttpContent
{
    public string Raw { get; }
    public long ByteLength { get; }
    public Dictionary<string, string[]> Headers { get; }
    public string? ContentType { get; }

    public HttpContent(
        string raw,
        long byteLength,
        Dictionary<string, string[]> headers,
        string? contentType = null)
    {
        Raw = raw;
        ByteLength = byteLength;
        Headers = headers;
        ContentType = contentType;
    }
}
