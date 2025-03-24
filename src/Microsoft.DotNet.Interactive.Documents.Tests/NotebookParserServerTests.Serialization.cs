// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.ParserServer;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public partial class NotebookParserServerTests
{
    public class Serialization
    {
        [Fact]
        public void Includes_kernel_info_for_well_known_kernels_when_document_text_contains_no_kernel_info_metadata()
        {
            var request = NotebookParserServerTestExtensions.CreateSerializeRequestFromJson();

            var response = NotebookParserServer.HandleRequest(request);

            var rawData = response
                          .Should()
                          .BeOfType<NotebookSerializeResponse>()
                          .Which
                          .RawData;

            var parseRequest = new NotebookParseRequest(
                "the-id",
                DocumentSerializationType.Dib,
                defaultLanguage: "csharp",
                rawData: rawData);

            var parseResponse = NotebookParserServer.HandleRequest(parseRequest);

            var kernelInfos = parseResponse
                              .Should()
                              .BeOfType<NotebookParseResponse>()
                              .Which
                              .Document.Metadata["kernelInfo"] as KernelInfoCollection;

            kernelInfos
                .Select(i => i.Name)
                .Should()
                .Contain([
                    "csharp",
                    "fsharp",
                    "pwsh",
                    "javascript",
                    "html",
                    "http",
                    "mermaid",
                    "value"
                ]);
        }

        [Fact]
        public void Includes_kernel_info_for_well_known_kernels_not_present_in_document_text()
        {
            var kernelInfoJson =
                """
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
                    }
                  ]
                }         
                """;

            var request = NotebookParserServerTestExtensions.CreateSerializeRequestFromJson(kernelInfoJson);

            var response = NotebookParserServer.HandleRequest(request);

            var rawData = response
                          .Should()
                          .BeOfType<NotebookSerializeResponse>()
                          .Which
                          .RawData;

            var parseRequest = new NotebookParseRequest(
                "the-id",
                DocumentSerializationType.Dib,
                defaultLanguage: "csharp",
                rawData: rawData);

            var parseResponse = NotebookParserServer.HandleRequest(parseRequest);

            var kernelInfos = parseResponse
                              .Should()
                              .BeOfType<NotebookParseResponse>()
                              .Which
                              .Document.Metadata["kernelInfo"] as KernelInfoCollection;

            kernelInfos
                .Select(i => i.Name)
                .Should()
                .Contain([
                    "csharp",
                    "fsharp",
                    "pwsh",
                    "javascript",
                    "html",
                    "http",
                    "mermaid",
                    "value",
                ]);
        }

        [Fact]
        public void Includes_kernel_info_for_custom_kernels_present_in_document_text()
        {
            var kernelInfoJson =
                """
                "kernelInfo": {
                  "defaultKernelName": "fsharp",
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
                      "name": "sql-adventureworks",
                      "languageName": "T-SQL"
                    }
                  ]
                }         
                """;

            var request = NotebookParserServerTestExtensions.CreateSerializeRequestFromJson(kernelInfoJson);

            var response = NotebookParserServer.HandleRequest(request);

            var rawData = response
                          .Should()
                          .BeOfType<NotebookSerializeResponse>()
                          .Which
                          .RawData;

            var parseRequest = new NotebookParseRequest(
                "the-id",
                DocumentSerializationType.Dib,
                defaultLanguage: "csharp",
                rawData: rawData);

            var parseResponse = NotebookParserServer.HandleRequest(parseRequest);

            var kernelInfos = parseResponse
                              .Should()
                              .BeOfType<NotebookParseResponse>()
                              .Which
                              .Document
                              .Metadata["kernelInfo"] as KernelInfoCollection;

            kernelInfos
                .Select(i => i.Name)
                .Should()
                .Contain("sql-adventureworks");

            kernelInfos.DefaultKernelName.Should().Be("fsharp");
        }

        [Theory]
        [InlineData(DocumentSerializationType.Dib, "#!csharp")]
        [InlineData(DocumentSerializationType.Ipynb, """
                                                     "kernelName": "csharp"
                                                     """)]
        public void It_can_serialize_file_based_on_document_type(
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
            var responseContent = response
                               .Should()
                               .BeOfType<NotebookSerializeResponse>()
                               .Which
                               .RawData
                               .AsUtf8String();

            responseContent
                .Should()
                .Contain(expected);
        }
    }
}