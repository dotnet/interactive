// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal partial class JupyterKernel : Kernel
{
    private readonly IJupyterKernelConnection _kernelConnection;
    private readonly CommsManager _commsManager;

    protected JupyterKernel(string name, IJupyterKernelConnection connection, string languageName, string languageVersion)
        : base(name, languageName, languageVersion)
    {
        _kernelConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        _commsManager = new CommsManager(Sender, Receiver);

        KernelInfo.RemoteUri = _kernelConnection.Uri;

        RegisterForDisposal(_kernelConnection);
        RegisterForDisposal(_commsManager);
    }

    internal IMessageReceiver Receiver => _kernelConnection.Receiver;

    internal IMessageSender Sender => _kernelConnection.Sender;

    public CommsManager Comms => _commsManager;

    private Task<T> RunOnKernelAsync<T>(RequestMessage content, CancellationToken token, string channel = MessageChannel.shell)
        where T : ReplyMessage
    {
        return RunOnKernelAsync<T>(content, Sender, Receiver, token, channel);
    }

    public static async Task<JupyterKernel> CreateAsync(string name, IJupyterKernelConnection connection)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        // start the kernel connection and request kernel info
        await connection.StartAsync();
        var kernelInfo = await RequestKernelInfo(connection);

        return new JupyterKernel(name,
                                 connection,
                                 kernelInfo?.LanguageInfo?.Name,
                                 kernelInfo?.LanguageInfo?.Version);
    }

    private static async Task<KernelInfoReply> RequestKernelInfo(IJupyterKernelConnection kernel)
    {
        var request = new KernelInfoRequest();
        var kernelInfoReply = await RunOnKernelAsync<KernelInfoReply>(request,
                                                                      kernel.Sender,
                                                                      kernel.Receiver,
                                                                      CancellationToken.None);

        return kernelInfoReply;
    }

    private async static Task<T> RunOnKernelAsync<T>(
        RequestMessage content,
        IMessageSender sender,
        IMessageReceiver receiver,
        CancellationToken token,
        string channel = MessageChannel.shell) where T : ReplyMessage
    {
        var request = Messaging.Message.Create(content, channel: channel);

        var reply = receiver.Messages.FilterByParent(request)
                                .SelectContent()
                                .OfType<T>()
                                .Take(1);

        await sender.SendAsync(request);
        var results = await reply.ToTask(token);

        return results;
    }
}
