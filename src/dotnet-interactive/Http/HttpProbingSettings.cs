// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Microsoft.DotNet.Interactive.Http;

internal class HttpProbingSettings
{
    public Uri[] AddressList { get; private set; }

    public static HttpProbingSettings Create(int? port, bool localOnlyNetworkInterfaces)
    {
        return new HttpProbingSettings
        {
            AddressList = GetProbingAddressList(port, localOnlyNetworkInterfaces)
        };
    }

    private static Uri[] GetProbingAddressList(int? httpPort, bool localOnlyNetworkInterfaces)
    {
        var sourcesIpAddresses = new HashSet<string>() {
            IPAddress.Loopback.ToString()
        };

        if (!localOnlyNetworkInterfaces)
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses.Select(a => a.Address.ToString()))
                    {
                        if (ip != IPAddress.Loopback.ToString() && !string.IsNullOrWhiteSpace(ip))
                        {
                            sourcesIpAddresses.Add(ip);
                        }
                    }
                }
            }

        var uriAddresses = sourcesIpAddresses
            .Select(ipAddress =>
            {
                var uriString = httpPort is not null ? $"http://{ipAddress}:{httpPort}/" : $"http://{ipAddress}/";

                if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                {
                    return uri;
                }

                return null;
            })
            .Where(u => u is not null)
            .ToArray();

        return uriAddresses;
    }
}