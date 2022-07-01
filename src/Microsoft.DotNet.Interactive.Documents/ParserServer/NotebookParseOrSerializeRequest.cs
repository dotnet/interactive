﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer;

public abstract class NotebookParseOrSerializeRequest
{
    public abstract RequestType Type { get; }
    public string Id { get; }
    public DocumentSerializationType SerializationType { get; }
    public string DefaultLanguage { get; }

    protected NotebookParseOrSerializeRequest(string id, DocumentSerializationType serializationType, string defaultLanguage)
    {
        Id = id;
        SerializationType = serializationType;
        DefaultLanguage = defaultLanguage;
    }

    public static NotebookParseOrSerializeRequest FromJson(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var request = JsonSerializer.Deserialize<NotebookParseOrSerializeRequest>(json, ParserServerSerializer.JsonSerializerOptions);
            
        return request ?? throw new InvalidOperationException();
    }
}