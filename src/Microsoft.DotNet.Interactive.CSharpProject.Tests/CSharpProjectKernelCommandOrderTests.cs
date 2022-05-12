﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class CSharpProjectKernelCommandOrderTests
    {
        public CSharpProjectKernelCommandOrderTests()
        {
            CSharpProjectKernel.RegisterEventsAndCommands();
        }

        [Fact]
        public async Task OpenDocument_before_OpenProject_fails()
        {
            var kernel = new CSharpProjectKernel("csharp");
            var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain($"Project must be opened, send the command '{nameof(OpenProject)}' first.");
        }

        [Fact]
        public async Task OpenProject_returns_a_full_list_of_available_project_items()
        {
            var kernel = new CSharpProjectKernel("csharp");
            var result = await kernel.SendAsync(new OpenProject(new Project(new[]
            {
                new ProjectFile("FileWithOneRegion.cs", @"
#region only-region
#endregion
"),
                new ProjectFile("FileWithNoRegions.cs", "class B { }"),
                new ProjectFile("FileWithTwoRegions.cs", @"
#region region-one
#endregion

#region region-two
#endregion
"),
            })));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<ProjectOpened>()
                .Which
                .ProjectItems
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ProjectItem("./FileWithNoRegions.cs", Array.Empty<string>(), new Dictionary<string, string>()),
                    new ProjectItem("./FileWithOneRegion.cs", new[] { "only-region" }, new Dictionary<string, string>{[ "only-region"]= string.Empty}),
                    new ProjectItem("./FileWithTwoRegions.cs", new[] { "region-one", "region-two" },new Dictionary<string, string>{[ "region-one"]= string.Empty,[ "region-two"]= string.Empty}),
                });
        }

        [Fact]
        public async Task OpenDocument_with_an_existing_file_path_succeeds()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// content") })));
            var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .NotContainErrors();
        }

        [Fact]
        public async Task OpenDocument_with_an_existing_file_path_produces_DocumentOpened_event()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// content") })));
            var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<DocumentOpened>(e => e.RelativeFilePath == "./Program.cs" && e.RegionName is null && e.Content == "// content");
        }

        [Fact]
        public async Task OpenDocument_with_an_existing_file_path_and_region_produces_DocumentOpened_event()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "#region region-name\n// content\n#endregion") })));
            var result = await kernel.SendAsync(new OpenDocument("Program.cs", "region-name"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<DocumentOpened>(e => e.RelativeFilePath == "./Program.cs" && e.RegionName == "region-name" && e.Content == "// content");
        }

        [Fact]
        public async Task OpenDocument_with_a_non_existing_file_path_succeeds()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("A_file_that_is_not_part_of_the_default_project.cs", "// content") })));
            var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .NotContainErrors();
        }

        [Fact]
        public async Task OpenDocument_with_a_non_existing_file_path_produces_DocumentOpened_event()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(Array.Empty<ProjectFile>())));
            var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<DocumentOpened>(e => e.RelativeFilePath == "./File_that_is_not_part_of_the_project.cs" && e.RegionName is null && e.Content == "");
        }

        [Fact]
        public async Task OpenDocument_with_a_non_existing_file_path_and_region_fails()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(Array.Empty<ProjectFile>())));
            var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs", regionName: "test-region"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain("Region 'test-region' not found in file './File_that_is_not_part_of_the_project.cs'");
        }

        [Fact]
        public async Task OpenDocument_with_path_and_region_name_succeeds()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region TEST_REGION
        var a = 123;
        #endregion
    }
}") })));
            var result = await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .NotContainErrors();
                
            kernelEvents
                .Should()
                .ContainSingle<DocumentOpened>()
                .Which.Content.Should().Be("var a = 123;");
        }

        [Fact]
        public async Task OpenDocument_with_region_name_fails_if_region_name_cannot_be_found()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region TEST_REGION
        #endregion
    }
}") })));
            var result = await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "NOT_A_REGION_IN_THE_FILE"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain("Region 'NOT_A_REGION_IN_THE_FILE' not found in file './Program.cs'");
        }

        [Fact]
        public async Task CompileProject_before_OpenProject_fails()
        {
            var kernel = new CSharpProjectKernel("csharp");
            var result = await kernel.SendAsync(new CompileProject());
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain($"Project must be opened, send the command '{nameof(OpenProject)}' first");
        }

        [Fact]
        public async Task CompileProject_before_OpenDocument_fails()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// file content") })));
            var result = await kernel.SendAsync(new CompileProject());
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain($"Document must be opened, send the command '{nameof(OpenDocument)}' first");
        }

        [Fact]
        public async Task RequestCompletions_before_OpenProject_fails()
        {
            var markedCode = @"in$$t x = 1;";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            var kernel = new CSharpProjectKernel("csharp");
            var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain($"Project must be opened, send the command '{nameof(OpenProject)}' first");
        }

        [Fact]
        public async Task RequestCompletions_before_OpenDocument_fails()
        {
            var markedCode = @"in$$t x = 1;";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// file content") })));
            var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain($"Document must be opened, send the command '{nameof(OpenDocument)}' first");
        }

        [Fact]
        public async Task CompletionsProduced_is_returned_when_the_entire_file_contents_are_set()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// this content will be replaced") })));
            await kernel.SendAsync(new OpenDocument("Program.cs"));

            var markedCode = @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        fileInfo.$$
    }
}";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
                .Should()
                .Contain(ci => ci.DisplayText == "AppendText");
        }

        [Fact]
        public async Task CompletionsProduced_is_returned_when_a_region_is_set()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
") })));
            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));

            var markedCode = @"fileInfo.$$";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
                .Should()
                .Contain(ci => ci.DisplayText == "AppendText");
        }

        [Fact]
        public async Task SignatureHelpProduced_is_returned_when_a_region_is_set()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
") })));
            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));

            var markedCode = @"fileInfo.CopyTo($$";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            var result = await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, character)));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            using var _ = new AssertionScope();
            var sigHelpProduced = kernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which;
            sigHelpProduced
                .ActiveSignatureIndex
                .Should()
                .Be(0);
            sigHelpProduced
                .ActiveParameterIndex
                .Should()
                .Be(0);
            sigHelpProduced
                .Signatures
                .Should()
                .BeEquivalentTo(new[]
                {
                    new SignatureInformation(
                        "FileInfo FileInfo.CopyTo(string destFileName)",
                        new FormattedValue("text/markdown", "Copies an existing file to a new file, disallowing the overwriting of an existing file."),
                        new[]
                        {
                            new ParameterInformation(
                                "string destFileName",
                                new FormattedValue("text/markdown", "**destFileName**: The name of the new file to copy to."))
                        }),
                    new SignatureInformation(
                        "FileInfo FileInfo.CopyTo(string destFileName, bool overwrite)",
                        new FormattedValue("text/markdown", "Copies an existing file to a new file, allowing the overwriting of an existing file."),
                        new[]
                        {
                            new ParameterInformation(
                                "string destFileName",
                                new FormattedValue("text/markdown", "**destFileName**: The name of the new file to copy to.")),
                            new ParameterInformation(
                                "bool overwrite",
                                new FormattedValue("text/markdown", "**overwrite**: true to allow an existing file to be overwritten; otherwise, false."))
                        })
                });
        }

        [Fact]
        public async Task SignatureHelpProduced_is_returned_when_no_region_is_set()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"// content will be replaced") })));
            await kernel.SendAsync(new OpenDocument("Program.cs"));

            var markedCode = @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        fileInfo.CopyTo($$
    }
}
";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            var result = await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, character)));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            using var _ = new AssertionScope();
            var sigHelpProduced = kernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which;
            sigHelpProduced
                .ActiveSignatureIndex
                .Should()
                .Be(0);
            sigHelpProduced
                .ActiveParameterIndex
                .Should()
                .Be(0);
            sigHelpProduced
                .Signatures
                .Should()
                .BeEquivalentTo(new[]
                {
                    new SignatureInformation(
                        "FileInfo FileInfo.CopyTo(string destFileName)",
                        new FormattedValue("text/markdown", "Copies an existing file to a new file, disallowing the overwriting of an existing file."),
                        new[]
                        {
                            new ParameterInformation(
                                "string destFileName",
                                new FormattedValue("text/markdown", "**destFileName**: The name of the new file to copy to."))
                        }),
                    new SignatureInformation(
                        "FileInfo FileInfo.CopyTo(string destFileName, bool overwrite)",
                        new FormattedValue("text/markdown", "Copies an existing file to a new file, allowing the overwriting of an existing file."),
                        new[]
                        {
                            new ParameterInformation(
                                "string destFileName",
                                new FormattedValue("text/markdown", "**destFileName**: The name of the new file to copy to.")),
                            new ParameterInformation(
                                "bool overwrite",
                                new FormattedValue("text/markdown", "**overwrite**: true to allow an existing file to be overwritten; otherwise, false."))
                        })
                });
        }

        [Fact]
        public async Task CompileProject_with_no_region_returns_an_assembly()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// this will be wholly replaced") })));
            await kernel.SendAsync(new OpenDocument("Program.cs"));
            await kernel.SendAsync(new SubmitCode(@"
public class Program
{
    public static void Main(string[] args)
    {
    }
}"));
            var result = await kernel.SendAsync(new CompileProject());
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<AssemblyProduced>()
                .Which
                .Assembly
                .Value
                .Length
                .Should()
                .BeGreaterThan(0);
        }

        [Fact]
        public async Task CompileProject_with_a_region_returns_an_assembly()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int x;
        #region TEST_REGION
        #endregion
    }
}") })));
            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));
            await kernel.SendAsync(new SubmitCode("x = 1;"));
            var result = await kernel.SendAsync(new CompileProject());
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            kernelEvents
                .Should()
                .ContainSingle<AssemblyProduced>()
                .Which
                .Assembly
                .Value
                .Length
                .Should()
                .BeGreaterThan(0);
        }

        [Fact]
        public async Task CompileProject_produces_diagnostics_and_fails()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt;
        #region test-region
        #endregion
    }
}
") })));
            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "test-region"));
            await kernel.SendAsync(new SubmitCode("someInt = \"this is a string\";"));
            var result = await kernel.SendAsync(new CompileProject());
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            using var _ = new AssertionScope();
            kernelEvents
                .Should()
                .ContainSingle<CommandFailed>();
            kernelEvents
                .Should()
                .NotContain(e => e is AssemblyProduced);
            kernelEvents
                .Should()
                .ContainSingle<DiagnosticsProduced>()
                .Which
                .Diagnostics
                .Should()
                .BeEquivalentTo(new[]
                {
                    new Diagnostic(
                        new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 28)),
                        CodeAnalysis.DiagnosticSeverity.Error,
                        "CS0029",
                        "(1,11): error CS0029: Cannot implicitly convert type 'string' to 'int'")
                });
        }

        [Fact]
        public async Task CompileProject_produces_empty_diagnostics_collection_when_it_passes()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
") })));
            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "test-region"));
            await kernel.SendAsync(new SubmitCode("int someInt = 1;"));
            var result = await kernel.SendAsync(new CompileProject());
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            using var _ = new AssertionScope();
            kernelEvents
                .Should()
                .NotContainErrors();
            kernelEvents
                .Should()
                .ContainSingle<AssemblyProduced>();
            kernelEvents
                .Should()
                .ContainSingle<DiagnosticsProduced>()
                .Which
                .Diagnostics
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task RequestDiagnostics_succeeds_even_with_errors_in_the_code()
        {
            var kernel = new CSharpProjectKernel("csharp");
            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") })));
            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "test-region"));
            var result = await kernel.SendAsync(new RequestDiagnostics("someInt = \"NaN\";"));
            var kernelEvents = result.KernelEvents.ToSubscribedList();
            using var _ = new AssertionScope();
            kernelEvents
                .Should()
                .NotContainErrors();
            kernelEvents
                .Should()
                .ContainSingle<DiagnosticsProduced>()
                .Which
                .Diagnostics
                .Should()
                .BeEquivalentTo(new[]
                {
                    new Diagnostic(
                        new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 15)),
                        CodeAnalysis.DiagnosticSeverity.Error,
                        "CS0029",
                        "(1,11): error CS0029: Cannot implicitly convert type 'string' to 'int'")
                });
        }
    }
}
