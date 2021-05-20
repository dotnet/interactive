// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Server;

#nullable enable
namespace Microsoft.DotNet.Interactive.Http
{
    public class SignalRConnectionOptions : KernelConnectionOptions
    {
        public string? HubUrl { get; set; }
    }

    public static class ConnectableKernel
    {
        public static KernelClient CreateKernelClient(this HubConnection hubConnection)
        {
            if (hubConnection is null) throw new ArgumentNullException(nameof(hubConnection));

            var input = new SignalRInputTextStream(hubConnection);
            var output = new SignalROutputTextStream(hubConnection);
            var kernelClient = new KernelClient(input, output);
            return kernelClient;
        }
    }
}