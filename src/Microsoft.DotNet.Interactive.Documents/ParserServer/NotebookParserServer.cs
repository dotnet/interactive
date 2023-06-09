// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer;

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
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var line = await Input.ReadLineAsync();
            if (line is not null)
            {
                NotebookParserServerResponse? response = null;
                string? requestId = null;
                try
                {
                    var request = NotebookParseOrSerializeRequest.FromJson(line);
                    requestId = request.Id;
                    response = HandleRequest(request);
                }
                catch (Exception ex)
                {
                    if (requestId is not null)
                    {
                        // if no ID could be parsed, there's no point in returning an error since it can't be associated with the request
                        response = new NotebookErrorResponse(requestId, ex.ToString());
                    }
                }

                if (response is not null)
                {
                    var responsePayload = response.ToJson();
                    await Output.WriteLineAsync(responsePayload);
                    await Output.FlushAsync();
                }
            }
        }
    }

    public static NotebookParserServerResponse HandleRequest(NotebookParseOrSerializeRequest request)
    {
        try
        {
            switch (request)
            {
                case NotebookParseRequest parse:
                {
                    using var contentStream = new MemoryStream(parse.RawData);
                    var document = request.SerializationType switch
                    {
                        DocumentSerializationType.Dib => CodeSubmission.Read(contentStream, WellKnownKernelInfos),
                        DocumentSerializationType.Ipynb => Notebook.Read(contentStream, WellKnownKernelInfos),
                        _ => throw new NotSupportedException($"Unable to parse an interactive document with type '{request.SerializationType }'"),
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
                    return new NotebookErrorResponse(request.Id, $"Unsupported request: {request}");
            }
        }
        catch (Exception ex)
        {
            return new NotebookErrorResponse(request.Id, ex.ToString());
        }
    }

    public void Dispose() => _cancellationTokenSource.Cancel();
}