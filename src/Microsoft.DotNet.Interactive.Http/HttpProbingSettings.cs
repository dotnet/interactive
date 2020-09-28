// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Microsoft.DotNet.Interactive.Http
{
    public class HttpProbingSettings
    {
        public Uri[] AddressList { get; private set; }

        public static HttpProbingSettings Create(int? port)
        {
            return new HttpProbingSettings
            {
                AddressList = GetProbingAddressList(port)
            };
        }

        private static Uri[] GetProbingAddressList(int? httpPort)
        {
            var sources = new List<string>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses.Select(a => a.Address.ToString()))
                    {

                        if (ip != IPAddress.Loopback.ToString())
                        {
                            sources.Add(ip);
                        }
                    }
                }
            }

            sources.Add(IPAddress.Loopback.ToString());

            var addresses = sources
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s =>
                {
                    var uriString = httpPort != null ? $"http://{s}:{httpPort}/" : $"http://{s}/";
                    if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                    {
                        return uri;
                    }

                    return null;
                })
                .Where(u => u != null)
                .ToArray();

            return addresses;
        }
    }
}