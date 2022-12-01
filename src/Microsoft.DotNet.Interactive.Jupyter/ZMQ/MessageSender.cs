// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;

using Microsoft.DotNet.Interactive.Jupyter.Messaging;

using NetMQ;

using Recipes;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

public class MessageSender
{
    private readonly IOutgoingSocket _socket;
    private readonly SignatureValidator _signatureValidator;
    private readonly Encoding _enc;
    private readonly object _lock;

    public MessageSender(IOutgoingSocket socket, SignatureValidator signatureValidator)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _enc = _signatureValidator.Encoding ?? new UTF8Encoding();
        _lock = new object();
    }

    public void Send(Message message)
    {
        // Translate everything before getting the lock.
        var hdr = Encode(message.Header);
        var par = Encode(message.ParentHeader);
        var md = Encode(message.MetaData);
        var cnt = Encode(message.Content);
        var hmac = _signatureValidator.CreateSignature(hdr, par, md, cnt);

        // Multiple channels (eg, control and shell) can send messages on separate threads.
        // Need to lock to avoid interleaving frames. Symptom of not doing this is random
        // signature exceptions in jupyter lab.
        lock (_lock)
        {
            if (message.Identifiers != null)
            {
                foreach (var ident in message.Identifiers)
                    _socket.SendFrame(ident.ToArray(), true);
            }

            _socket.SendFrame("<IDS|MSG>", true);
            _socket.SendFrame(hmac, true);
            _socket.SendFrame(hdr, true);
            _socket.SendFrame(par, true);
            _socket.SendFrame(md, true);
            _socket.SendFrame(cnt, false);
        }
    }

    private byte[] Encode(object val)
    {
        var str = (val ?? new object()).ToJson();
        return _enc.GetBytes(str);
    }
}