// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Http;

[TypeFormatterSource(
    typeof(HttpResponseFormatterSource),
    PreferredMimeTypes = [HtmlFormatter.MimeType])]
public class PartialHttpResponse : EmptyHttpResponse
{
    public int StatusCode { get; }
    public string ReasonPhrase { get; }
    public string Version { get; }
    public double? ElapsedMilliseconds { get; internal set; }
    public long? ContentByteLength { get; }

    public PartialHttpResponse(
        int statusCode,
        string reasonPhrase,
        string version,
        double? elapsedMilliseconds = null,
        long? contentByteLength = null)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Version = version;
        ElapsedMilliseconds = elapsedMilliseconds;
        ContentByteLength = contentByteLength;
    }
}
