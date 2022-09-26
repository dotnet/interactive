// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public class ImportNotebookTests : DocumentFormatTestsBase
{
    [Theory]
    [InlineData(".ipynb", ".ipynb")]
    [InlineData(".ipynb", ".dib")]
    [InlineData(".dib", ".ipynb")]
    [InlineData(".dib", ".dib")]
    public void It_can_import_one_notebook_into_another(
        string importingNotebookExtension,
        string importedNotebookExtension)
    {
        var importedCode = "// imported code";

        var importedNotebook = new InteractiveDocument
        {
            new InteractiveDocumentElement(importedCode, "csharp")
        };

        var importedNotebookPath = Path.GetTempFileName() + importedNotebookExtension;

        switch (importedNotebookExtension)
        {
            case ".dib":
                File.WriteAllText(importedNotebookPath, importedNotebook.ToCodeSubmissionContent());
                break;
            case ".ipynb":
                File.WriteAllText(importedNotebookPath, importedNotebook.ToJupyterJson());
                break;
        }

        var importingNotebook =
            new InteractiveDocument
            {
                new InteractiveDocumentElement($"#!import {importedNotebookPath}"),
                new InteractiveDocumentElement("// code in importing notebook", "csharp")
            };

        var importingNotebookText = importingNotebookExtension switch
        {
            ".dib" => importingNotebook.ToCodeSubmissionContent(),
            ".ipynb" => importingNotebook.ToJupyterJson()
        };

        var document = CodeSubmission.Parse(
            importingNotebookText, 
            DefaultKernelInfos);

        document.GetImportedDocuments().Should().ContainSingle()
                .Which
                .Elements.Should().ContainSingle()
                .Which
                .Contents.Should().Contain(importedCode);
    }

    [Fact]
    public void Imported_documents_override_parent_document_kernel_aliases()
    {
        

        // TODO (Imported_documents_override_parent_document_kernel_aliases) write test
        throw new NotImplementedException();
    }

    [Fact]
    public void Imported_document_metadata_augments_parent_document_kernel_info()
    {
        

        // TODO (Imported_document_metadata_augments_parent_document_kernel_info) write test
        throw new NotImplementedException();
    }

    [Fact]
    public void It_can_import_a_file_that_imports_another_file()
    {
        // TODO (It_can_import_a_file_that_imports_another_file) write test
        throw new NotImplementedException();
    }
}