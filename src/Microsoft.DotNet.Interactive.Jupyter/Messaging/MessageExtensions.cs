// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Recipes;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging;

public static class MessageExtensions
{
    public static bool IsEmptyJson(string source) => Regex.IsMatch(source, @"^\s*{\s*}\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public static T DeserializeFromJsonString<T>(string source)
    {
        var ret = default(T);
        if (!string.IsNullOrWhiteSpace(source) && !IsEmptyJson(source))
        {
            ret = JsonSerializer.Deserialize<T>(source, JsonSerializationExtensions.SerializerOptions);
        }
        return ret;
    }

    private static Protocol.Message DeserializeMessageContentFromJsonString(string source, string messageType)
    {
        var ret = Protocol.Message.Empty;
        if (!string.IsNullOrWhiteSpace(source)) 
        {
            ret = Protocol.Message.FromJsonString(source, messageType);
        }
        return ret;
    }

    public static Message DeserializeMessage(string signature, string headerJson, string parentHeaderJson,
        string metadataJson, string contentJson, IReadOnlyList<IReadOnlyList<byte>> identifiers, JsonSerializerOptions options = null)
    {
        var header = JsonSerializer.Deserialize<Header>(headerJson, options ?? JsonSerializationExtensions.SerializerOptions);
        var parentHeader = DeserializeFromJsonString<Header>(parentHeaderJson);
        var metaData = MetadataExtensions.DeserializeMetadataFromJsonString(metadataJson);
        var content = DeserializeMessageContentFromJsonString(contentJson, header.MessageType);

        var message = new Message(header, content, parentHeader, signature, metaData, identifiers);

        return message;
    }
}