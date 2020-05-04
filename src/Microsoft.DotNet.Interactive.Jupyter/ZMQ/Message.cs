// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public class Message
    {
        [JsonIgnore]
        public IReadOnlyList<IReadOnlyList<byte>> Identifiers { get; }

        [JsonIgnore]
        public string Signature { get; }

        [JsonProperty("header")]
        public Header Header { get; }

        [JsonProperty("parent_header")]
        public Header ParentHeader { get; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public Protocol.Message Content { get; }

        [JsonProperty("buffers")]
        public IReadOnlyList<IReadOnlyList<byte>> Buffers { get; }

        public Message(Header header,
            Protocol.Message content = null,
            Header parentHeader = null,
            string signature = null,
            IReadOnlyDictionary<string, object> metaData = null,
            IReadOnlyList<IReadOnlyList<byte>> identifiers = null,
            IReadOnlyList<IReadOnlyList<byte>> buffers = null)
        {
            Header = header;
            ParentHeader = parentHeader;
            Buffers = buffers ?? new List<IReadOnlyList<byte>>();
            Identifiers = identifiers ?? new List<IReadOnlyList<byte>>();
            MetaData = metaData ?? new Dictionary<string, object>();
            Content = content ?? Protocol.Message.Empty;
            Signature = signature ?? string.Empty;
        }

        public static Message Create<T>(T content,
            Header parentHeader = null,
            IReadOnlyList<IReadOnlyList<byte>> identifiers = null,
            IReadOnlyDictionary<string, object> metaData = null,
            string signature = null)
            where T : Protocol.Message
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var session = parentHeader?.Session ?? Guid.NewGuid().ToString();
            var header = Header.Create(content, session);
            var message = new Message(header, parentHeader: parentHeader, content: content, identifiers: identifiers, signature: signature, metaData: metaData);

            return message;
        }

        public static Message CreateReply<T>(
            T content,
            Message request)
            where T : ReplyMessage
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request.Content is RequestMessage) && request.Content != Protocol.Message.Empty)
            {
                throw new ArgumentOutOfRangeException($"{request.Content.GetType()} is not a valid {nameof(RequestMessage)}");
            }

            var replyMessage = Create(content, request.Header, request.Identifiers, request.MetaData, request.Signature);

            return replyMessage;
        }

        public static Message CreatePubSub<T>(
            T content,
            Message request,
            string kernelIdentity = null)
            where T : PubSubMessage
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request.Content is RequestMessage) && request.Content != Protocol.Message.Empty)
            {
                throw new ArgumentOutOfRangeException($"{request.Content.GetType()} is nor a valid {nameof(RequestMessage)}");
            }

            var topic = Topic(content, kernelIdentity);
            var identifiers = topic == null ? null : new[] { Topic(content, kernelIdentity) };
            var replyMessage = Create(content, request.Header, identifiers: identifiers, metaData: request.MetaData, signature: request.Signature);

            return replyMessage;
        }


        private static byte[] Topic<T>(T content, string kernelIdentity) where T : PubSubMessage
        {
            byte[] encodedTopic;
            var name = content.GetType().Name;
            switch (name)
            {

                case nameof(Status):
                    {
                        var fullTopic = GenerateFullTopic("status");
                        encodedTopic = Encoding.Unicode.GetBytes(fullTopic);
                    }
                    break;

                case nameof(ExecuteInput):
                    {
                        var fullTopic = GenerateFullTopic("execute_input");
                        encodedTopic = Encoding.Unicode.GetBytes(fullTopic);
                    }
                    break;

                case nameof(DisplayData):
                case nameof(UpdateDisplayData):
                    encodedTopic = Encoding.Unicode.GetBytes("display_data");
                    break;

                case nameof(ExecuteResult):
                    encodedTopic = Encoding.Unicode.GetBytes("execute_result");
                    break;

                case nameof(Error):
                    encodedTopic = null;
                    break;

                case nameof(Stream):
                    {
                        if (!(content is Stream stream))
                        {
                            throw new ArgumentNullException(nameof(stream));
                        }
                        encodedTopic = Encoding.Unicode.GetBytes($"stream.{stream.Name}");
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"type {name} is not supported");

            }

            string GenerateFullTopic(string topic)
            {
                if (kernelIdentity == null)
                {
                    throw new ArgumentNullException(nameof(kernelIdentity));
                }
                return $"kernel.{kernelIdentity}.{topic}";
            }

            return encodedTopic;
        }
    }
}