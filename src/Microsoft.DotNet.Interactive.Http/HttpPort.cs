// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Http
{
    public class HttpPort
    {
        public static HttpPort Auto { get; } = new HttpPort();

        public HttpPort(int portNumber)
        {
            PortNumber = portNumber;
        }

        private HttpPort()
        {

        }

        public int? PortNumber { get; }

        public bool IsAuto => PortNumber == null;
    }
}