// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal partial class JupyterKernel : Kernel
{
    private readonly IMessageSender _sender;
    private readonly IMessageReceiver _receiver;

    protected JupyterKernel(string name, IMessageSender sender, IMessageReceiver receiver, string languageName, string languageVersion)
        : base(name)
    {
        KernelInfo.LanguageName = languageName;
        KernelInfo.LanguageVersion = languageVersion;
        KernelInfo.DisplayName = $"{name} - {languageName} {languageVersion} (Preview)";
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
    }

    private IMessageReceiver Receiver => _receiver;

    private IMessageSender Sender => _sender;

    private Task<T> RunOnKernelAsync<T>(RequestMessage content, CancellationToken token, string channel = MessageChannelValues.shell)
        where T : ReplyMessage
    {
        return RunOnKernelAsync<T>(content, Sender, Receiver, token, channel);
    }

    public static async Task<JupyterKernel> CreateAsync(string name, IMessageSender sender, IMessageReceiver receiver)
    {
        if (sender == null) throw new ArgumentNullException(nameof(sender));
        if (receiver == null) throw new ArgumentNullException(nameof(receiver));

        // request kernel info
        var kernelInfo = await RequestKernelInfo(sender, receiver);

        return new JupyterKernel(name,
                                 sender,
                                 receiver,
                                 kernelInfo?.LanguageInfo?.Name,
                                 kernelInfo?.LanguageInfo?.Version);
    }

    private static async Task<KernelInfoReply> RequestKernelInfo(IMessageSender sender, IMessageReceiver receiver)
    {
        var request = new KernelInfoRequest();
        var kernelInfoReply = await RunOnKernelAsync<KernelInfoReply>(request,
                                                                      sender,
                                                                      receiver,
                                                                      CancellationToken.None);

        return kernelInfoReply;
    }

    private async static Task<T> RunOnKernelAsync<T>(
        RequestMessage content,
        IMessageSender sender,
        IMessageReceiver receiver,
        CancellationToken token,
        string channel = MessageChannelValues.shell) where T : ReplyMessage
    {
        var request = Messaging.Message.Create(content, channel: channel);

        var reply = receiver.Messages.ResponseOf(request)
                                .Content()
                                .OfType<T>()
                                .Take(1);

        await sender.SendAsync(request);
        var results = await reply.ToTask(token);

        return results;
    }
}
