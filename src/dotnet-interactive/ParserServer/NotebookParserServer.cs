// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.App.ParserServer.NotebookParserServer>;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

public class NotebookParserServer : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public NotebookParserServer(TextReader input, TextWriter output)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public TextReader Input { get; }

    public TextWriter Output { get; }

    public static KernelInfoCollection WellKnownKernelInfos = new()
    {
        new("csharp", languageName: "C#", aliases: new[] { "c#", "cs" }),
        new("fsharp", languageName: "F#", aliases: new[] { "f#", "fs" }),
        new("pwsh", languageName: "PowerShell", aliases: new[] { "powershell" }),
        new("javascript", languageName: "JavaScript", aliases: new[] { "js" }),
        new("html", languageName: "HTML"),
        new("sql", languageName: "SQL"),
        new("kql", languageName: "KQL"),
        new("mermaid", languageName: "Mermaid"),
        new("http", languageName: "HTTP"),
        new("value"),
    };

    public async Task RunAsync()
    {
        using var op = Log.OnEnterAndConfirmOnExit();

        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var line = await Input.ReadLineAsync(_cancellationTokenSource.Token);

            if (line is not null)
            {
                NotebookParserServerResponse? response;
                NotebookParseOrSerializeRequest? request;

                try
                {
                    request = NotebookParseOrSerializeRequest.FromJson(line);
                }
                catch (Exception ex)
                {
                    op.Error("Exception while parsing {line}", ex, line);
                    continue;
                }

                try
                {
                    response = HandleRequest(request);
                }
                catch (Exception ex)
                {
                    op.Error("Exception while handling request with id {requestId}: {request}", ex, request.Id, request);
                    break;
                }

                var responsePayload = response.ToJson();
                await Output.WriteLineAsync(responsePayload);
                await Output.FlushAsync();
            }
        }

        op.Succeed();
    }

    public static NotebookParserServerResponse HandleRequest(NotebookParseOrSerializeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        switch (request)
        {
            case NotebookParseRequest parse:
            {
                using var contentStream = new MemoryStream(parse.RawData);
                var document = request.SerializationType switch
                {
                    DocumentSerializationType.Dib => CodeSubmission.Read(contentStream, WellKnownKernelInfos),
                    DocumentSerializationType.Ipynb => Notebook.Read(contentStream, WellKnownKernelInfos),
                    _ => throw new NotSupportedException($"Unable to parse an interactive document with type '{request.SerializationType}'"),
                };
                return new NotebookParseResponse(request.Id, document);
            }

            case NotebookSerializeRequest serialize:
            {
                using var resultStream = new MemoryStream();
                switch (request.SerializationType)
                {
                    case DocumentSerializationType.Dib:
                        CodeSubmission.Write(serialize.Document, resultStream, WellKnownKernelInfos);
                        break;
                    case DocumentSerializationType.Ipynb:
                        Notebook.Write(serialize.Document, resultStream, WellKnownKernelInfos);
                        break;
                    default:
                        throw new NotSupportedException($"Unable to serialize a interactive document of type '{request.SerializationType}'");
                }

                resultStream.Position = 0;
                var resultArray = resultStream.ToArray();
                return new NotebookSerializeResponse(request.Id, resultArray);
            }

            default:
                throw new IndexOutOfRangeException($"Request type not supported: {request.GetType()}");
        }
    }

    public void Dispose() => _cancellationTokenSource.Cancel();
}