// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;

internal class CommsManager : IDisposable
{
    private readonly Dictionary<string, ICommTarget> _targets = new();
    private readonly Dictionary<string, CommAgent> _agents = new();

    private readonly IMessageSender _sender;
    private readonly IMessageReceiver _receiver;

    private readonly CompositeDisposable _disposables;

    public CommsManager(IMessageSender messageSender, IMessageReceiver messageReceiver)
    {
        _receiver = messageReceiver;
        _sender = messageSender;

        var subscription = messageReceiver.Messages.Subscribe(async (message) =>
        {
            if (message.Content is CommClose closeComm)
            {
                if (_agents.ContainsKey(closeComm.CommId))
                {
                    _agents.Remove(closeComm.CommId);
                }
            }

            if (message.Content is CommOpen commOpen)
            {
                await HandleCommOpenRequestAsync(commOpen);
            }
        });

        _disposables = new CompositeDisposable
        {
            subscription
        };
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public async Task<CommAgent> OpenCommAsync(string targetName, string commId = null, IReadOnlyDictionary<string, object> data = null)
    {
        var agent = AddAgent(commId);

        await _sender.SendAsync(Message.Create(new CommOpen(agent.CommId, targetName, data)));

        return agent;
    }

    private async Task HandleCommOpenRequestAsync(CommOpen commOpen)
    {
        if (string.IsNullOrEmpty(commOpen.TargetName) || string.IsNullOrEmpty(commOpen.CommId) ||
            !_targets.TryGetValue(commOpen.TargetName, out ICommTarget target))
        {
            var commClose = new CommClose(commOpen.CommId, new Dictionary<string, object> 
            {
                { "reason", $"Comm target '{commOpen.TargetName}' is not registered on the client"}
            });

            await _sender.SendAsync(Message.Create(commClose));
            return;
        }

        var agent = AddAgent(commOpen.CommId);
        target.OnCommOpen(agent, commOpen.Data);
    }

    private CommAgent AddAgent(string commId)
    {
        CommAgent agent;

        if (string.IsNullOrEmpty(commId) || !_agents.TryGetValue(commId, out agent))
        {
            agent = new CommAgent(commId ?? Guid.NewGuid().ToString(),
                              _sender, _receiver);
        }
        else
        {
            // FIX: (AddAgent) this looks like it should be an exception
        }

        _agents.Add(agent.CommId, agent);

        return agent;
    }
}
