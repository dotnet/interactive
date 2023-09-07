// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Http;

public class ConnectSignalRCommand : ConnectKernelCommand
{
    public ConnectSignalRCommand() : base("signalr", "Connects to a kernel using SignalR")
    {
        AddOption(HubUrlOption);
    }

    public Option<Uri> HubUrlOption { get; } =
        new("--hub-url",
            "The URL of the SignalR hub")
        {
            IsRequired = true
        };

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var hubUrl = commandLineContext.ParseResult.GetValueForOption(HubUrlOption);

        var connector = new SignalRKernelConnector(hubUrl);

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var kernel = await connector.CreateKernelAsync(localName);
        return new[] { kernel };
    }
}