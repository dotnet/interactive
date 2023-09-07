// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;

internal class CommAgent : IDisposable
{
    private readonly IMessageSender _sender;
    private readonly string _commId;
    private readonly Subject<Protocol.Message> _commChannel = new();

    private readonly CompositeDisposable _disposables;

    public CommAgent(string commId, IMessageSender messageSender, IMessageReceiver messageReceiver)
    {
        _commId = commId ?? throw new ArgumentNullException(nameof(commId));
        _sender = messageSender;

        var subscription = messageReceiver.Messages
            .Subscribe(message =>
        {
            if (message.Content is CommClose closeComm && closeComm.CommId == _commId)
            {
                IsClosed = true;
                _commChannel.OnNext(closeComm);
            }

            if (message.Content is CommMsg commMsg && commMsg.CommId == _commId)
            {
                _commChannel.OnNext(commMsg);
            }
        });

        _disposables = new CompositeDisposable
        {
            _commChannel,
            subscription
        };
    }

    public string CommId => _commId;

    public bool IsClosed { get; private set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public async Task SendAsync(IReadOnlyDictionary<string, object> data)
    {
        await _sender.SendAsync(Messaging.Message.Create(new CommMsg(_commId, data)));
    }

    public async Task CloseAsync(IReadOnlyDictionary<string, object> data)
    {
        await _sender.SendAsync(Messaging.Message.Create(new CommClose(_commId, data)));
    }

    public IObservable<Protocol.Message> Messages => _commChannel;
}
