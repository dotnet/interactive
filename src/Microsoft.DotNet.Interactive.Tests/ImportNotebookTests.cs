// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class ImportNotebookTests
{
    [TestMethod]
    [DataRow(".ipynb")]
    [DataRow(".dib")]
    public async Task It_imports_and_runs_well_known_polyglot_file_formats(string notebookExt)
    {
        using var kernel = new CompositeKernel { 
                new CSharpKernel(),
                new FSharpKernel(),
                new HtmlKernel()
            }
            .UseImportMagicCommand();

        var document = new InteractiveDocument
        {
            new InteractiveDocumentElement
            {
                Contents = "6+5",
                KernelName = "csharp"
            },
            new InteractiveDocumentElement
            {
                Contents = "5+3",
                KernelName = "markdown" //should not evaluate to 8
            },
            new InteractiveDocumentElement
            {
                Contents = "5+3",
                KernelName = "html" //should not evaluate to 8
            },
            new InteractiveDocumentElement
            {
                Contents = "11*2",
                KernelName = "fsharp"
            },
            new InteractiveDocumentElement
            {
                Contents = "11*3",
                KernelName = "csharp"
            }
        };

        var notebookContents = notebookExt switch
        {
            ".ipynb" => document.ToJupyterJson(),
            ".dib" => document.ToCodeSubmissionContent(),
            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {notebookExt}")
        };
            
        var filePath = $@".\testnotebook{notebookExt}";

        await File.WriteAllTextAsync(filePath, notebookContents);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!import \"{filePath}\"");

        events.Should().NotContainErrors();

        var returnedValues = events.Where(x => x.GetType() == typeof(ReturnValueProduced)).ToArray();
            
        int[] results = [11, 22, 33];
        returnedValues.Length.Should().Be(results.Length);

        for (int i=0 ; i < results.Length; i++)
        {
            ((ReturnValueProduced)returnedValues[i]).Value.Should().Be(results[i]);
        }
    }

    [TestMethod]
    [DataRow(".cs")]
    [DataRow(".csx")]
    [DataRow(".fs")]
    [DataRow(".fsx")]
    [DataRow(".ps1")]
    [DataRow(".http")]
    public async Task It_imports_and_runs_source_code_from_files_with_well_known_file_extensions(string fileExtension)
    {
        using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FSharpKernel(),
                new PowerShellKernel(),
                new HttpKernel()
            }
            .UseImportMagicCommand();

        string receivedCodeSubmissions = "";
        string receivedTargetKernelName = null;

        kernel.AddMiddleware((command, context, next) =>
        {
            if (command is SubmitCode submitCode)
            {
                receivedCodeSubmissions += submitCode.Code;
                receivedTargetKernelName = command.TargetKernelName;
            }

            return next(command, context);
        });

        var (fileContents, expectedTargetKernelName) = fileExtension switch
        {
            ".cs" or ".csx" => ("Console.WriteLine(123);", "csharp"),
            ".fs" or ".fsx" => ("123 |> System.Console.WriteLine", "fsharp"),
            ".ps1" => ("123 | Out-Default", "pwsh"),
            ".http" => ("@url = https://httpbin.org/", "http"),
            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {fileExtension}")
        };

        using var directory = DisposableDirectory.Create();

        var filePath = Path.Combine(directory.Directory.FullName, $"file{fileExtension}");

        await File.WriteAllTextAsync(filePath, fileContents);

        using var events = kernel.KernelEvents.ToSubscribedList();

        var code = $"#!import \"{filePath}\"";

        await kernel.SubmitCodeAsync(code);

        using var _ = new AssertionScope();

        events.Should().NotContainErrors();
        receivedCodeSubmissions.Should().Contain(fileContents);
        receivedTargetKernelName.Should().Be(expectedTargetKernelName);
    }

    [TestMethod]
    [DataRow(".ipynb")]
    [DataRow(".dib")]
    public async Task It_produces_DisplayedValueProduced_events_for_markdown_cells(string notebookExt)
    {
        using var kernel = new CompositeKernel {
                new CSharpKernel(),
                new FSharpKernel(),
                new HtmlKernel(),
            }
            .UseImportMagicCommand();

        var document = new InteractiveDocument
        {
            new InteractiveDocumentElement
            {
                Contents = "6+5",
                KernelName = "csharp"
            },
            new InteractiveDocumentElement
            {
                Contents = "5+11",
                KernelName = "markdown" //should not evaluate to 8
            },
            new InteractiveDocumentElement
            {
                Contents = "5+3",
                KernelName = "html" //should not evaluate to 8
            },
            new InteractiveDocumentElement
            {
                Contents = "11*2",
                KernelName = "fsharp"
            },
            new InteractiveDocumentElement
            {
                Contents = "11*3",
                KernelName = "csharp"
            }
        };

        var notebookContents = notebookExt switch
        {
            ".ipynb" => document.ToJupyterJson(),
            ".dib" => document.ToCodeSubmissionContent(),
            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {notebookExt}")
        };

        var filePath = $@".\testnotebook{notebookExt}";

        await File.WriteAllTextAsync(filePath, notebookContents);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!import {filePath}");

        events.Should().ContainSingle<DisplayedValueProduced>(v => v.FormattedValues.Any(f => f.MimeType == "text/markdown"));
    }

    [TestMethod]
    [DataRow(".ipynb")]
    [DataRow(".dib")]
    public async Task It_loads_packages_from_imports(string notebookExt)
    {
        using var kernel = new CompositeKernel {
                new CSharpKernel().UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseValueSharing(),
                new FSharpKernel(),
                new HtmlKernel(),
            }
            .UseImportMagicCommand();

        var document = new InteractiveDocument
        {
            new InteractiveDocumentElement
            {
                Contents = "#r \"nuget: Microsoft.DotNet.Interactive.AIUtilities, 1.0.0-beta.23517.1\"",
                KernelName = "csharp"
            }
        };

        var notebookContents = notebookExt switch
        {
            ".ipynb" => document.ToJupyterJson(),
            ".dib" => document.ToCodeSubmissionContent(),
            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {notebookExt}")
        };

        var filePath = $@".\testnotebook{notebookExt}";

        await File.WriteAllTextAsync(filePath, notebookContents);

        var result = await kernel.SubmitCodeAsync($"#!import {filePath}");

        result.Events.Should().NotContainErrors();
    }
}