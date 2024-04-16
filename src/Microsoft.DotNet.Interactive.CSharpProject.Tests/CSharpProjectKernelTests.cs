// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[Collection(nameof(PrebuildFixture))]
[LogToPocketLogger]
public class CSharpProjectKernelTests
{
    private readonly ITestOutputHelper _output;
    private readonly CompositeDisposable _disposables = new();

    public CSharpProjectKernelTests(ITestOutputHelper output)
    {
        CSharpProjectKernel.RegisterEventsAndCommands();

        _output = output;
        _disposables.Add(_output.SubscribeToPocketLogger());
    }

    [Fact]
    public async Task OpenDocument_before_OpenProject_fails()
    {
        using var kernel = new CSharpProjectKernel();
        
        var result = await kernel.SendAsync(new OpenDocument("Program.cs"));

        result.Events
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
        using var kernel = new CSharpProjectKernel();
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
var a = 2;
#endregion
"),
        })));

        result.Events
              .Should()
              .ContainSingle<ProjectOpened>()
              .Which
              .ProjectItems
              .Should()
              .BeEquivalentTo(new[]
              {
                  new ProjectItem("./FileWithNoRegions.cs", Array.Empty<string>(), new Dictionary<string, string>()),
                  new ProjectItem("./FileWithOneRegion.cs", new[] { "only-region" }, new Dictionary<string, string>{[ "only-region"]= string.Empty}),
                  new ProjectItem("./FileWithTwoRegions.cs", new[] { "region-one", "region-two" },new Dictionary<string, string>{[ "region-one"]= string.Empty,[ "region-two"]= "var a = 2;"}),
              });
    }

    [Fact]
    public async Task OpenProject_overrides_previously_loaded_project()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[]
        {
            new ProjectFile("program.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Program {
    class Program {
        static void Main(string[] args){
            #region controller

            #endregion
        }
    }
}")
        })));
            
        var result = await kernel.SendAsync(new OpenDocument("program.cs", "controller"));
            
        result.Events
            .Should()
            .ContainSingle<DocumentOpened>()
            .Which
            .Content
            .Should()
            .BeNullOrWhiteSpace();

        await kernel.SendAsync(new OpenProject(new Project(new[]
        {
            new ProjectFile("program.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Program {
    class Program {
        static void Main(string[] args){
            #region controller
            Console.WriteLine(123);
            #endregion
        }
    }
}")
        })));

        result = await kernel.SendAsync(new OpenDocument("program.cs", "controller"));

        result.Events
            .Should()
            .ContainSingle<DocumentOpened>()
            .Which
            .Content
            .Should()
            .Contain("Console.WriteLine(123);");            
    }

    [Fact]
    public async Task OpenDocument_with_an_existing_file_path_succeeds()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// content") })));
        var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
        result.Events
            .Should()
            .NotContainErrors();
    }

    [Fact]
    public async Task OpenDocument_with_an_existing_file_path_produces_DocumentOpened_event()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// content") })));
        var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
        result.Events
            .Should()
            .ContainSingle<DocumentOpened>(e => e.RelativeFilePath == "./Program.cs" && e.RegionName is null && e.Content == "// content");
    }

    [Fact]
    public async Task OpenDocument_with_an_existing_file_path_and_region_produces_DocumentOpened_event()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "#region region-name\n// content\n#endregion") })));
        var result = await kernel.SendAsync(new OpenDocument("Program.cs", "region-name"));
        result.Events
            .Should()
            .ContainSingle<DocumentOpened>(e => e.RelativeFilePath == "./Program.cs" && e.RegionName == "region-name" && e.Content == "// content");
    }

    [Fact]
    public async Task OpenDocument_with_a_non_existing_file_path_succeeds()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("A_file_that_is_not_part_of_the_default_project.cs", "// content") })));
        
        var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs"));
        
        result.Events
            .Should()
            .NotContainErrors();
    }

    [Fact]
    public async Task OpenDocument_with_a_non_existing_file_path_produces_DocumentOpened_event()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(Array.Empty<ProjectFile>())));
        var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs"));
        result.Events
              .Should()
              .ContainSingle<DocumentOpened>(e => e.RelativeFilePath == "./File_that_is_not_part_of_the_project.cs" && e.RegionName is null && e.Content == "");
    }

    [Fact]
    public async Task OpenDocument_with_a_non_existing_file_path_and_region_fails()
    {
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(Array.Empty<ProjectFile>())));
        var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs", regionName: "test-region"));

        result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        result.Events
              .Should()
              .NotContainErrors();
                
        result.Events
              .Should()
              .ContainSingle<DocumentOpened>()
              .Which.Content.Should().Be("var a = 123;");
    }

    [Fact]
    public async Task OpenDocument_with_region_name_fails_if_region_name_cannot_be_found()
    {
        using var kernel = new CSharpProjectKernel();
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

        result.Events
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
        using var kernel = new CSharpProjectKernel();

        var result = await kernel.SendAsync(new CompileProject());

        result.Events
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
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// file content") })));

        var result = await kernel.SendAsync(new CompileProject());
        
        result.Events
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
        using var kernel = new CSharpProjectKernel();

        var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));
        
        result.Events
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
        using var kernel = new CSharpProjectKernel();
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// file content") })));
        
        var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        
        using var _ = new AssertionScope();
        
        var sigHelpProduced = result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        using var _ = new AssertionScope();

        var sigHelpProduced = result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        result.Events
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
        using var kernel = new CSharpProjectKernel();
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

        result.Events
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
        using var kernel = new CSharpProjectKernel();
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
        using var _ = new AssertionScope();
        result.Events
              .Should()
              .ContainSingle<CommandFailed>();
        result.Events
              .Should()
              .NotContain(e => e is AssemblyProduced);
        result.Events
              .Should()
              .ContainSingle<DiagnosticsProduced>()
              .Which
              .Diagnostics
              .Should()
              .ContainEquivalentOf(
                  new Diagnostic(
                      new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 28)),
                      CodeAnalysis.DiagnosticSeverity.Error,
                      "CS0029",
                      "(1,11): error CS0029: Cannot implicitly convert type 'string' to 'int'")
              );
    }

    [Fact]
    public async Task CompileProject_produces_empty_diagnostics_collection_when_it_passes()
    {
        using var kernel = new CSharpProjectKernel();
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
        using var _ = new AssertionScope();
        result.Events
              .Should()
              .NotContainErrors();
        result.Events
              .Should()
              .ContainSingle<AssemblyProduced>();
        result.Events
              .Should()
              .ContainSingle<DiagnosticsProduced>()
              .Which
              .Diagnostics
              .Should()
              .NotContain(d => d.Severity == CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task RequestDiagnostics_succeeds_even_with_errors_in_the_code()
    {
        using var kernel = new CSharpProjectKernel();
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

        using var _ = new AssertionScope();
        result.Events
              .Should()
              .NotContainErrors();
        result.Events
              .Should()
              .ContainSingle<DiagnosticsProduced>()
              .Which
              .Diagnostics
              .Should()
              .ContainEquivalentOf(
                  new Diagnostic(
                      new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 15)),
                      CodeAnalysis.DiagnosticSeverity.Error,
                      "CS0029",
                      "(1,11): error CS0029: Cannot implicitly convert type 'string' to 'int'")
              );
    }

    [Fact]
    public async Task project_files_are_case_insensitive()
    {
        using var kernel = new CSharpProjectKernel();

        // the console project defaults to a file named `Program.cs` so by specifying `program.cs` we're
        // effectively adding a duplicate file
        await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("program.cs", @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Program
{
  class Program
  {
    static void Main(string[] args)
    {
      #region controller

      #endregion
    }
  }
}") })));
        await kernel.SendAsync(new OpenDocument("./program.cs", regionName: "controller"));
        await kernel.SendAsync(new SubmitCode("var a = 123;"));
        var kernelResult = await kernel.SendAsync(new CompileProject());

        using var _ = new AssertionScope();

        kernelResult.Events
                    .Should()
                    .NotContainErrors();

        kernelResult.Events
                    .Should()
                    .ContainSingle<AssemblyProduced>()
                    .Which
                    .Assembly
                    .Value
                    .Should()
                    .NotBeNullOrWhiteSpace();
    }
}