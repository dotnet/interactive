// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Document.Tests
{
    public class RoundTripNotebookDocumentFileFormatTests : NotebookDocumentFileFormatTestsBase
    {

        [Theory]
        [InlineData(".dib", false)]
        [InlineData(".dotnet-interactive", false)]
        [InlineData(".ipynb", true)]
        public void notebook_document_can_be_round_tripped_through_supported_file_formats(string extension, bool checkOutputs)
        {
            // not all interactive types retain outputs, so round-tripping them isn't necessarily interesting
            var outputs = checkOutputs
                ? new[]
                {
                    new InteractiveDocumentDisplayOutputElement(new Dictionary<string, object>
                    {
                        { "text/html", "This is html." }
                    })
                }
                : Array.Empty<InteractiveDocumentOutputElement>();
            var cells = new[]
            {
                new InteractiveDocumentElement("csharp", "//", outputs)
            };
            var originalNotebook = new InteractiveDocument(cells);
            var fileName = $"interactive{extension}";
            using var stream = new MemoryStream();
            NotebookFileFormatHandler.Write(fileName, originalNotebook, "\n", stream);
            stream.Position = 0;
            var roundTrippedNotebook = NotebookFileFormatHandler.Read(fileName, stream, "csharp", KernelLanguageAliases);
            roundTrippedNotebook
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(originalNotebook);
        }
    }
}
