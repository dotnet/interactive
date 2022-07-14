// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public class Header
    {
        [JsonPropertyName("msg_id")]
        public string MessageId { get; }

        [JsonPropertyName("username")]
        public string Username { get; }

        [JsonPropertyName("session")]
        public string Session { get; }

        [JsonPropertyName("date")]
        public string Date { get; }

        [JsonPropertyName("msg_type")]
        public string MessageType
        {
            get;
        }

        [JsonPropertyName("version")]
        public string Version { get; }

        public Header(string messageType, string messageId, string version, string session, string username, string date = null)
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(messageType));
            }

            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(messageId));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(version));
            }

            MessageType = messageType;
            MessageId = messageId;
            Version = version;
            Session = session;
            Username = username;
            Date = date ?? DateTime.UtcNow.ToString("o");
        }

        public static Header Create<T>(T messageContent, string session)
            where T : Protocol.Message
        {
            if (messageContent is null)
            {
                throw new ArgumentNullException(nameof(messageContent));
            }

            return Create(messageContent.MessageType, session);
        }

        public static Header Create<T>(T messageContent, Session session)
            where T : Protocol.Message
        {
            if (messageContent is null)
            {
                throw new ArgumentNullException(nameof(messageContent));
            }
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            return Create(messageContent.MessageType, session.Id, session.ProtocolVersion, session.Username);
        }

        public static Header Create<T>(string session)
            where T : Protocol.Message
        {
            var messageType = Protocol.Message.GetMessageType(typeof(T));
            return Create(messageType, session);
        }

        private static Header Create(string messageType, string session)
        {
            return Create(messageType, session, Constants.MESSAGE_PROTOCOL_VERSION, Constants.USERNAME);
        }

        private static Header Create(string messageType, string session, string protocolVersion, string username)
        {
            var newHeader = new Header(
                messageType: messageType,
                messageId: Guid.NewGuid().ToString(),
                version: protocolVersion,
                username: username,
                session: session);

            return newHeader;
        }
    }
}
