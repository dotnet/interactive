// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

public static class NotebookParserServerExtensions
{
    public static string ToJson(this NotebookParserServerResponse response)
    {
        var text = JsonSerializer.Serialize(response, response.GetType(), App.ParserServer.ParserServerSerializer.JsonSerializerOptions);
        return text;
    }

    public static string ToJson(this NotebookParseOrSerializeRequest request)
    {
        var text = JsonSerializer.Serialize(request, request.GetType(), App.ParserServer.ParserServerSerializer.JsonSerializerOptions);
        return text;
    }
}