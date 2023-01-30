// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging;

public class Message
{
    [JsonIgnore]
    public IReadOnlyList<IReadOnlyList<byte>> Identifiers { get; }

    [JsonIgnore]
    public string Signature { get; }

    [JsonPropertyName("header")]
    public Header Header { get; }

    [JsonPropertyName("parent_header")]
    public Header ParentHeader { get; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object> MetaData { get; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Protocol.Message Content { get; }

    [JsonPropertyName("buffers")]
    public IReadOnlyList<IReadOnlyList<byte>> Buffers { get; }

    // Used over remote websocket to determine underlying channel to send to
    [JsonPropertyName("channel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Channel { get; }

    public Message(Header header,
        Protocol.Message content = null,
        Header parentHeader = null,
        string signature = null,
        IReadOnlyDictionary<string, object> metaData = null,
        IReadOnlyList<IReadOnlyList<byte>> identifiers = null,
        IReadOnlyList<IReadOnlyList<byte>> buffers = null,
        string channel = MessageChannelValues.shell)
    {
        Header = header;
        ParentHeader = parentHeader;
        Buffers = buffers ?? new List<IReadOnlyList<byte>>();
        Identifiers = identifiers ?? new List<IReadOnlyList<byte>>();
        MetaData = metaData ?? new Dictionary<string, object>();
        Content = content ?? Protocol.Message.Empty;
        Signature = signature ?? string.Empty;
        Channel = channel;
    }

    public static Message Create<T>(T content,
        Header parentHeader = null,
        IReadOnlyList<IReadOnlyList<byte>> identifiers = null,
        IReadOnlyDictionary<string, object> metaData = null,
        string signature = null,
        string channel = MessageChannelValues.shell)
        where T : Protocol.Message
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var session = parentHeader?.Session ?? Guid.NewGuid().ToString();
        var header = Header.Create(content, session);
        var message = new Message(header, parentHeader: parentHeader, content: content, identifiers: identifiers, signature: signature, metaData: metaData, channel: channel);

        return message;
    }

    // request/reply work on both shell or control channel. 
    public static Message CreateReply<T>(
        T content,
        Message request,
        string channel = MessageChannelValues.shell)
        where T : ReplyMessage
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var replyMessage = Create(content, request.Header, request.Identifiers, request.MetaData, request.Signature, channel: channel);

        return replyMessage;
    }

    public static Message CreatePubSub<T>(
        T content,
        Message request,
        string kernelIdentity = null)
        where T : PubSubMessage
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        var topic = Topic(content, kernelIdentity);
        var identifiers = topic is null ? null : new[] { Topic(content, kernelIdentity) };
        var replyMessage = Create(content, request.Header, identifiers: identifiers, metaData: request.MetaData, signature: request.Signature, channel: MessageChannelValues.iopub);

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

            case nameof(Protocol.Stream):
                {
                    if (!(content is Protocol.Stream stream))
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
            if (kernelIdentity is null)
            {
                throw new ArgumentNullException(nameof(kernelIdentity));
            }
            return $"kernel.{kernelIdentity}.{topic}";
        }

        return encodedTopic;
    }
}