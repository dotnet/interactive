// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.ParserServer;
using Nerdbank.Streams;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public class NotebookParserServerTests_TextInterface : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task Notebook_parser_server_takes_request_until_newline_and_returns_response_on_a_single_line(string newline)
    {
        var request = new NotebookParseRequest(
            "the-id",
            serializationType: DocumentSerializationType.Dib,
            defaultLanguage: "csharp",
            rawData: Encoding.UTF8.GetBytes("#!csharp\nvar x = 1;"));
        var requestJson = request.ToJson();

        var asyncEnumerator = GetResponseObjectsEnumerable(requestJson + newline).GetAsyncEnumerator();

        var response = await asyncEnumerator.GetNextAsync();
        response
            .Should()
            .BeOfType<NotebookParseResponse>()
            .Which
            .Should()
            .BeEquivalentTo(new NotebookParseResponse(
                                "the-id",
                                document: new InteractiveDocument(
                                    new List<InteractiveDocumentElement>
                                    {
                                        new("var x = 1;", "csharp")
                                    })));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task Notebook_parser_server_can_handle_repeated_requests_with_different_newlines(string newline)
    {
        var request1 = new NotebookParseRequest(
            "the-id",
            serializationType: DocumentSerializationType.Dib,
            defaultLanguage: "csharp",
            rawData: Encoding.UTF8.GetBytes("#!csharp\nvar x = 1;"));
        var request2 = new NotebookSerializeRequest(
            "the-second-id",
            serializationType: DocumentSerializationType.Dib,
            defaultLanguage: "csharp",
            newLine: "\n",
            document: new InteractiveDocument
            (
                new List<InteractiveDocumentElement>
                {
                    new("let y = 2", "fsharp")
                }));
        var fullRequestText = string.Concat(
            request1.ToJson(),
            newline,
            request2.ToJson(),
            newline);

        var asyncEnumerator = GetResponseObjectsEnumerable(fullRequestText).GetAsyncEnumerator();

        var result1 = await asyncEnumerator.GetNextAsync();
        result1
            .Should()
            .BeOfType<NotebookParseResponse>()
            .Which
            .Id
            .Should()
            .Be("the-id");

        var result2 = await asyncEnumerator.GetNextAsync();
        result2
            .Should()
            .BeOfType<NotebookSerializeResponse>()
            .Which
            .Id
            .Should()
            .Be("the-second-id");
    }

    [Fact]
    public async Task Notebook_parser_server_does_nothing_with_garbage_input_and_skips_to_the_next_line_to_process()
    {
        var validRequest = new NotebookParseRequest(
            "the-id",
            serializationType: DocumentSerializationType.Dib,
            defaultLanguage: "csharp",
            rawData: Array.Empty<byte>());

        var requestJson = string.Concat(
            "this is a request that can't be handled in any meaningful way, including returning an error",
            "\n",
            validRequest.ToJson(),
            "\n");

        var asyncEnumerator = GetResponseObjectsEnumerable(requestJson).GetAsyncEnumerator();

        var result = await asyncEnumerator.GetNextAsync();
        result
            .Should()
            .BeOfType<NotebookParseResponse>()
            .Which
            .Id
            .Should()
            .Be("the-id");
    }

    private async IAsyncEnumerable<NotebookParserServerResponse> GetResponseObjectsEnumerable(string inputText)
    {
        using var input = new StringReader(inputText);
        var stream = new SimplexStream();
        var output = new StreamWriter(stream);
        var server = new NotebookParserServer(input, output);
        _disposables.Add(server);

        var _ = Task.Run(() => server.RunAsync()); // start server listener in the background

        var outputReader = new StreamReader(stream);

        while (true)
        {
            var responseText = await outputReader.ReadLineAsync();
            if (responseText is { })
            {
                var responseObject = NotebookParserServerResponse.FromJson(responseText);
                yield return responseObject;
            }
            else
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}

internal static class IAsyncEnumeratorExtensions
{
    public static async Task<T> GetNextAsync<T>(this IAsyncEnumerator<T> collection)
    {
        var itemsRemain = await collection.MoveNextAsync();
        if (itemsRemain)
        {
            return collection.Current;
        }

        throw new InvalidOperationException("No more items");
    }
}