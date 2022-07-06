// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    public class NotebookParserServer
    {
        public TextReader Input { get; }
        public TextWriter Output { get; }

        public static KernelNameCollection WellKnownKernelNames = new()
        {
            new("csharp", new[] { "c#", "C#", "cs" }),
            new("fsharp", new[] { "f#", "F#", "fs" }),
            new("pwsh", new[] { "powershell" }),
            new("javascript", new[] { "js" }),
            new("html", Array.Empty<string>()),
            new("sql", Array.Empty<string>()),
            new("kql", Array.Empty<string>()),
        };

        public NotebookParserServer(TextReader input, TextWriter output)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public async Task RunAsync()
        {
            while (true)
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
                                DocumentSerializationType.Dib => CodeSubmission.Read(contentStream, request.DefaultLanguage, WellKnownKernelNames),
                                DocumentSerializationType.Ipynb => Notebook.Read(contentStream, WellKnownKernelNames),
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
                                    CodeSubmission.Write(serialize.Document, resultStream);
                                    break;
                                case DocumentSerializationType.Ipynb:
                                    Notebook.Write(serialize.Document, resultStream);
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
    }
}
