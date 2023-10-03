// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Http;

public class HttpOptions
{
    public bool EnableHttpApi { get; }
    public HttpPort HttpPort { get; }

    public HttpOptions(bool enableHttpApi, HttpPort httpPort)
    {
        EnableHttpApi = enableHttpApi;
        HttpPort = httpPort;
    }
}