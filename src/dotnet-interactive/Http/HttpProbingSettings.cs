// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Microsoft.DotNet.Interactive.Http;

public class HttpProbingSettings
{
    public IEnumerable<string> AddressList { get; private set; }

    public static HttpProbingSettings Create(int? httpPort, Func<NetworkInterface[]> getAllNetworkInterfaces)
    {
        var ipAddress = getAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Select(x => x.Address.ToString())
                .Append(IPAddress.Loopback.ToString())
                .ToHashSet();

        var uriAddresses = ipAddress
            .Select(AddHttpPort(httpPort));

        return new HttpProbingSettings
        {
            AddressList = uriAddresses
        };
    }

    private static Func<string, string> AddHttpPort(int? httpPort)
    {
        if (httpPort is null)
            return ipAddress => $"http://{ipAddress}/";

        return ipAddress => $"http://{ipAddress}:{httpPort}/";
    }
}