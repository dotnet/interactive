// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    public abstract class NotebookParserServerResponse
    {
        public string Id { get; }

        public NotebookParserServerResponse(string id)
        {
            Id = id;
        }

        public static NotebookParserServerResponse FromJson(string json)
        {
            var request = JsonSerializer.Deserialize<NotebookParserServerResponse>(json, ParserServerSerializer.JsonSerializerOptions);
            return request;
        }
    }
}
