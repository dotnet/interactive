// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

[TestClass]
public class ImportNotebookTests : DocumentFormatTestsBase
{
    [TestMethod]
    [DataRow(".ipynb", ".ipynb")]
    [DataRow(".ipynb", ".dib")]
    [DataRow(".dib", ".ipynb")]
    [DataRow(".dib", ".dib")]
    public async Task It_can_read_one_notebooks_imports_into_another(
        string importingNotebookExtension,
        string importedFileExtension)
    {
        var importedCode = "// imported code";

        var importedNotebook = new InteractiveDocument
        {
            new InteractiveDocumentElement(importedCode, "csharp")
        };

        var importedNotebookPath = Path.GetTempFileName() + importedFileExtension;

        switch (importedFileExtension)
        {
            case ".dib":
                await File.WriteAllTextAsync(importedNotebookPath, importedNotebook.ToCodeSubmissionContent());
                break;
            case ".ipynb":
                await File.WriteAllTextAsync(importedNotebookPath, importedNotebook.ToJupyterJson());
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

        var document = importingNotebookExtension switch
        {
            ".dib" => CodeSubmission.Parse(importingNotebookText, DefaultKernelInfos),
            ".ipynb" => Notebook.Parse(importingNotebookText, DefaultKernelInfos)
        };

        var importedDocuments = await document.GetImportsAsync(ParseDirectiveLine).ToArrayAsync();

        importedDocuments.Should().ContainSingle()
                         .Which
                         .Elements.Should().ContainSingle()
                         .Which
                         .Contents.Should().Contain(importedCode);
    }

    [TestMethod]
    [DataRow(".cs", "csharp")]
    [DataRow(".csx", "csharp")]
    [DataRow(".fs", "fsharp")]
    [DataRow(".fsx", "fsharp")]
    [DataRow(".ps1", "pwsh")]
    [DataRow(".html", "html")]
    [DataRow(".js", "javascript")]
    public async Task It_can_import_language_specific_source_files_into_a_notebook(
        string importedFileExtension,
        string expectedKernelName)
    {
        var importedFile = "// imported code";
        
        var importedFilePath = Path.GetTempFileName() + importedFileExtension;

        await File.WriteAllTextAsync(importedFilePath, importedFile);

        var importingNotebook =
            new InteractiveDocument
            {
                new InteractiveDocumentElement($"#!import {importedFilePath}"),
                new InteractiveDocumentElement("// code in importing notebook", "csharp")
            };

        var importingNotebookText = importingNotebook.ToJupyterJson();

        var document = Notebook.Parse(importingNotebookText, DefaultKernelInfos);

        var importedDocuments = await document.GetImportsAsync(ParseDirectiveLine).ToArrayAsync();

        var importedElement = importedDocuments.Should().ContainSingle()
                                               .Which
                                               .Elements.Should().ContainSingle()
                                               .Which;

        importedElement.Contents.Should().Be(importedFile);
        importedElement.KernelName.Should().Be(expectedKernelName);
    }

    [TestMethod]
    public async Task It_can_import_a_file_that_imports_another_file()
    {
        // given notebook1 which imports notebook2 which imports notebook3...

        var notebook3Path = Path.GetTempFileName() + ".dib";
        var notebook3 = new InteractiveDocument
        {
            new InteractiveDocumentElement("// notebook3 content", "csharp")
        };
        await File.WriteAllTextAsync(notebook3Path, notebook3.ToCodeSubmissionContent());

        var notebook2Path = Path.GetTempFileName() + ".dib";
        var notebook2 =
            new InteractiveDocument
            {
                new InteractiveDocumentElement($"#!import {notebook3Path}"),
                new InteractiveDocumentElement("// notebook2 content", "csharp")
            };
        await File.WriteAllTextAsync(notebook2Path, notebook2.ToCodeSubmissionContent());

        var notebook1 =
            new InteractiveDocument
            {
                new InteractiveDocumentElement($"#!import {notebook2Path}"),
                new InteractiveDocumentElement("// notebook1 content", "csharp")
            };

        // round trip notebook1 through the parser
        notebook1 = CodeSubmission.Parse(notebook1.ToCodeSubmissionContent(), DefaultKernelInfos);

        var importedDocuments = await notebook1.GetImportsAsync(ParseDirectiveLine, recursive: true).ToArrayAsync();

        importedDocuments.Should().HaveCount(2);

        importedDocuments.ElementAt(0).Elements.Should().ContainSingle()
                         .Which
                         .Contents.Should().Contain("notebook2 content");

        importedDocuments.ElementAt(1).Elements.Should().ContainSingle()
                         .Which
                         .Contents.Should().Contain("notebook3 content");
    }

    [TestMethod]
    public async Task When_imported_document_does_not_exist_then_it_throws()
    {
        var missingNotebookPath = "not-found.dib";

        var notebook =
            new InteractiveDocument
            {
                new InteractiveDocumentElement($"#!import {missingNotebookPath}"),
                new InteractiveDocumentElement("// notebook1 content", "csharp")
            };

        // round trip notebook1 through the parser
        notebook = CodeSubmission.Parse(notebook.ToCodeSubmissionContent(), DefaultKernelInfos);

        await notebook.Invoking(async n => await n.GetImportsAsync(ParseDirectiveLine).ToArrayAsync())
                      .Should()
                      .ThrowAsync<FileNotFoundException>()
                      .WithMessage("Could not find file*not-found.dib*");
    }
}