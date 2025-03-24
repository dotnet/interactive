// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.ParserServer;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public partial class NotebookParserServerTests
{
    public class Serialization
    {
        [Fact]
        public void Notebook_parser_server_can_handle_serialize_requests()
        {
            var request = """
                          {
                            "type": "serialize",
                            "id": "2",
                            "serializationType": "dib",
                            "defaultLanguage": "csharp",
                            "newLine": "\r\n",
                            "document": {
                              "elements": [      
                                {
                                  "executionOrder": 0,
                                  "kernelName": "csharp",
                                  "contents": "#r \"nuget: DotLanguage.InteractiveExtension, *-*\"",
                                  "outputs": [
                                  ]
                                },
                                {
                                  "executionOrder": 0,
                                  "kernelName": "dot",
                                  "contents": "digraph Blah {\r\n    rankdir=\"LR\"\r\n    node [shape=\"box\"];\r\n    A -> B -> C;\r\n    B -> D;\r\n  }",
                                  "outputs": []
                                }
                              ],
                              "metadata": {
                                "kernelInfo": {
                                  "defaultKernelName": "csharp",
                                  "items": [
                                    {
                                      "name": "csharp",
                                      "languageName": "C#",
                                      "aliases": [
                                        "c#",
                                        "cs"
                                      ]
                                    },
                                    {
                                      "name": "fsharp",
                                      "languageName": "F#",
                                      "aliases": [
                                        "f#",
                                        "fs"
                                      ]
                                    },
                                    {
                                      "name": "pwsh",
                                      "languageName": "PowerShell",
                                      "aliases": [
                                        "powershell"
                                      ]
                                    },
                                    {
                                      "name": "javascript",
                                      "languageName": "JavaScript",
                                      "aliases": [
                                        "js"
                                      ]
                                    },
                                    {
                                      "name": "html",
                                      "languageName": "HTML",
                                      "aliases": [ ]
                                    },
                                    {
                                      "name": "sql",
                                      "languageName": "SQL",
                                      "aliases": []
                                    },
                                    {
                                      "name": "kql",
                                      "languageName": "KQL",
                                      "aliases": []
                                    },
                                    {
                                      "name": "mermaid",
                                      "languageName": "Mermaid",
                                      "aliases": []
                                    },
                                    {
                                      "name": "value",
                                      "aliases": []
                                    },
                                    {
                                      "name": "dot",
                                      "languageName": "dotlang",
                                      "aliases": []
                                    }
                                  ]
                                }
                              }
                            }
                          }

                          """;

            var response = NotebookParserServer.HandleRequest(NotebookParseOrSerializeRequest.FromJson(request));

            var raw = response
                      .Should()
                      .BeOfType<NotebookSerializeResponse>()
                      .Which
                      .RawData;

            var parseRequest = new NotebookParseRequest(
                "the-id",
                DocumentSerializationType.Dib,
                defaultLanguage: "csharp",
                rawData: raw);

            var parseResponse = NotebookParserServer.HandleRequest(parseRequest);

            var kernelInfos = parseResponse
                              .Should()
                              .BeOfType<NotebookParseResponse>()
                              .Which
                              .Document.Metadata["kernelInfo"] as KernelInfoCollection;

            kernelInfos
                .Should()
                .NotBeNull();
        }

        [Theory]
        [InlineData(DocumentSerializationType.Dib, "#!csharp")]
        [InlineData(DocumentSerializationType.Ipynb, """
                                                     "cell_type"
                                                     """)]
        public void Notebook_parser_server_can_serialize_file_based_on_document_type(
            DocumentSerializationType serializationType, 
            string expected)
        {
            var request = new NotebookSerializeRequest(
                "the-id",
                serializationType,
                defaultLanguage: "csharp",
                newLine: "\n",
                document: new InteractiveDocument(new List<InteractiveDocumentElement>
                {
                    new("var x = 1;", "csharp")
                })
            );
            var response = NotebookParserServer.HandleRequest(request);
            response
                .Should()
                .BeOfType<NotebookSerializeResponse>()
                .Which
                .RawData
                .AsUtf8String()
                .Should()
                .Contain(expected);
        }
    }
}