// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    public abstract class NotebookParserServerResponse
    {
        protected NotebookParserServerResponse(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public static NotebookParserServerResponse FromJson(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var request = JsonSerializer.Deserialize<NotebookParserServerResponse>(json, ParserServerSerializer.JsonSerializerOptions);

            return request ?? throw new InvalidOperationException();
        }
    }
}