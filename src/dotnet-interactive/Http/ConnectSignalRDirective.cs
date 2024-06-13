// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Http;

public class ConnectSignalRDirective : ConnectKernelDirective<ConnectSignalR>
{
    public ConnectSignalRDirective() : base("signalr", "Connects to a kernel using SignalR")
    {
        AddOption(HubUrlParameter);
    }

    public KernelDirectiveParameter HubUrlParameter { get; } =
        new("--hub-url",
            "The URL of the SignalR hub")
        {
            Required = true
        };

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectSignalR connectCommand,
        KernelInvocationContext context)
    {
        var hubUrl = connectCommand.HubUrl;

        var connector = new SignalRKernelConnector(hubUrl);

        var localName = connectCommand.ConnectedKernelName;

        var kernel = await connector.CreateKernelAsync(localName);

        return new[] { kernel };
    }
}