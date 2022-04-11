// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Connection;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class ConnectHost
{
    public static CompositeKernel ConnectInProcessHost(
        this CompositeKernel localCompositeKernel
        , Uri uri = null)
    {
        CompositeKernel remoteCompositeKernel = new();

        localCompositeKernel.RegisterForDisposal(remoteCompositeKernel);

        ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel,
            uri ?? new Uri("kernel://local/"), 
            new Uri("kernel://remote/"));

        return localCompositeKernel;
    }

    public static void ConnectInProcessHost(
        CompositeKernel localCompositeKernel,
        CompositeKernel remoteCompositeKernel,
        Uri localHostUri = null,
        Uri remoteHostUri = null)
    {
        localHostUri ??= new("kernel://local");
        remoteHostUri ??= new("kernel://remote");

        var innerLocalReceiver = new BlockingCommandAndEventReceiver(localHostUri);

        var localReceiver = new MultiplexingKernelCommandAndEventReceiver(innerLocalReceiver);

        var remoteInnerReceiver = new BlockingCommandAndEventReceiver(remoteHostUri);

        var localSender = remoteInnerReceiver.CreateSender(localHostUri);

        var remoteReceiver = new MultiplexingKernelCommandAndEventReceiver(remoteInnerReceiver);

        var remoteSender = innerLocalReceiver.CreateSender(remoteHostUri);

        var localHost = localCompositeKernel.UseHost(
            localSender,
            localReceiver,
            localHostUri);

        var remoteHost = remoteCompositeKernel.UseHost(
            remoteSender,
            remoteReceiver,
            remoteHostUri);

        var _ = localHost.ConnectAsync();
        var __ = remoteHost.ConnectAsync();

        localCompositeKernel.RegisterForDisposal(localHost);
        localCompositeKernel.RegisterForDisposal(localReceiver);
        remoteCompositeKernel.RegisterForDisposal(remoteHost);
        remoteCompositeKernel.RegisterForDisposal(remoteReceiver);
    }
}