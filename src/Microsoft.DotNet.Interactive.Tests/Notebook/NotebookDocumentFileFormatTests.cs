// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.DotNet.Interactive.Notebook;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Notebook
{
    public partial class NotebookDocumentFileFormatTests : LanguageKernelTestBase
    {
        public NotebookDocumentFileFormatTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public NotebookDocument ParseFromString(string fileName, string content)
        {
            using var kernel = CreateCompositeKernel();
            var rawData = Encoding.UTF8.GetBytes(content);
            var notebook = kernel.ParseNotebook(fileName, rawData);
            return notebook;
        }

        public string SerializeToString(string fileName, NotebookDocument notebook, string newline = "\r\n")
        {
            var rawData = NotebookFileFormatHandler.Serialize(fileName, notebook, newline);
            var content = Encoding.UTF8.GetString(rawData);
            return content;
        }
    }
}
