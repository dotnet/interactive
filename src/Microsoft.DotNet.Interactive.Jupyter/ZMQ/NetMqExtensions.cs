// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    internal static class NetMQExtensions
    {
        private static T DeserializeFromJsonString<T>(string source)
        {
            var ret = default(T);
            if (!string.IsNullOrWhiteSpace(source))
            {
                var sourceObject = JObject.Parse(source);
                if (sourceObject.HasValues)
                {
                    ret = JsonConvert.DeserializeObject<T>(source);
                }
            }
            return ret;
        }

        private static Protocol.Message DeserializeMessageContentFromJsonString(string source, string messageType)
        {
            var ret = Protocol.Message.Empty;
            if (!string.IsNullOrWhiteSpace(source))
            {
                var sourceObject = JObject.Parse(source);
                if (sourceObject.HasValues)
                {
                    ret = Protocol.Message.FromJsonString(source, messageType);
                }
            }
            return ret;
        }

        public static Message GetMessage(this NetMQSocket socket)
        {
            // There may be additional ZMQ identities attached; read until the delimiter <IDS|MSG>"
            // and store them in message.identifiers
            // http://ipython.org/ipython-doc/dev/development/messaging.html#the-wire-protocol
            var delimiterAsBytes = Encoding.ASCII.GetBytes(Constants.DELIMITER);

            var identifiers = new List<byte[]>();
            while (true)
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

            var message = DeserializeMessage(signature, headerJson, parentHeaderJson, metadataJson,  contentJson, identifiers);
            return message;
        }

        public static Message DeserializeMessage(string signature, string headerJson, string parentHeaderJson,
            string metadataJson, string contentJson, IReadOnlyList<IReadOnlyList<byte>> identifiers)
        {
            var header = JsonConvert.DeserializeObject<Header>(headerJson);
            var parentHeader = DeserializeFromJsonString<Header>(parentHeaderJson);
            var metaData = DeserializeFromJsonString<Dictionary<string, object>>(metadataJson) ?? new Dictionary<string, object>();
            var content = DeserializeMessageContentFromJsonString(contentJson, header.MessageType);

            var message = new Message(header, content, parentHeader, signature, metaData, identifiers);

            return message;
        }
    }
}