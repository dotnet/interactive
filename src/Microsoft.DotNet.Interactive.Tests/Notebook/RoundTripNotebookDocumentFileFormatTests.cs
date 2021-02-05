// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Notebook;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Notebook
{
    public class RoundTripNotebookDocumentFileFormatTests : NotebookDocumentFileFormatTests
    {
        public RoundTripNotebookDocumentFileFormatTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory]
        [InlineData(".dib", false)]
        [InlineData(".dotnet-interactive", false)]
        [InlineData(".ipynb", true)]
        public void notebook_document_can_be_round_tripped_through_supported_file_formats(string extension, bool checkOutputs)
        {
            // not all notebook types retain outputs, so round-tripping them isn't necessarily interesting
            var outputs = checkOutputs
                ? new[]
                {
                    new NotebookCellDisplayOutput(new Dictionary<string, object>()
                    {
                        { "text/html", "This is html." }
                    })
                }
                : Array.Empty<NotebookCellOutput>();
            var cells = new[]
            {
                new NotebookCell("csharp", "//", outputs)
            };
            var originalNotebook = new NotebookDocument(cells);
            var fileName = $"notebook{extension}";
            var content = SerializeToString(fileName, originalNotebook);
            var roundTrippedNotebook = ParseFromString(fileName, content);
            roundTrippedNotebook
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(originalNotebook);
        }
    }
}
