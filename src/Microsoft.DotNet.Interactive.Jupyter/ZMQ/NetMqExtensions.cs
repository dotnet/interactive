// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using NetMQ;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

public static class NetMQExtensions
{
    public static Message GetMessage(this NetMQSocket socket)
    {
        // There may be additional ZMQ identities attached; read until the delimiter <IDS|MSG>"
        // and store them in message.identifiers
        // http://ipython.org/ipython-doc/dev/development/messaging.html#the-wire-protocol
        var delimiterAsBytes = Encoding.ASCII.GetBytes(JupyterConstants.DELIMITER);

        var identifiers = new List<byte[]>();
        while (!socket.IsDisposed)
        {
            var delimiter = socket.ReceiveFrameBytes();
            if (delimiter.SequenceEqual(delimiterAsBytes))
            {
                break;
            }
            identifiers.Add(delimiter);
        }

        // Getting Hmac
        var signature = socket.ReceiveFrameString();
           
        // Getting Header
        var headerJson = socket.ReceiveFrameString();

        // Getting parent header
        var parentHeaderJson = socket.ReceiveFrameString();

        // Getting metadata
        var metadataJson = socket.ReceiveFrameString();

        // Getting content
        var contentJson = socket.ReceiveFrameString();

        var message = MessageExtensions.DeserializeMessage(signature, headerJson, parentHeaderJson, metadataJson,  contentJson, identifiers);
        return message;
    }
}