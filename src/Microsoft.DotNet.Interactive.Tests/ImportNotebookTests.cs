// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class ImportNotebookTests
{
    [Theory]
    [InlineData(".ipynb")]
    [InlineData(".dib")]
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

        await kernel.SubmitCodeAsync($"#!import {filePath}");

        var returnedValues = events.Where(x => x.GetType() == typeof(ReturnValueProduced)).ToArray();
            
        int[] results = new int[] { 11, 22, 33 };
        returnedValues.Length.Should().Be(results.Length);

        for (int i=0 ; i < results.Length; i++)
        {
            ((ReturnValueProduced)returnedValues[i]).Value.Should().Be(results[i]);
        }
    }

    [Theory]
    [InlineData(".ipynb")]
    [InlineData(".dib")]
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

    [Theory]
    [InlineData(".ipynb")]
    [InlineData(".dib")]
    public async Task It_load_packages_from_imports(string notebookExt)
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

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!import {filePath}");

        events.Should().NotContainErrors();
    }
}