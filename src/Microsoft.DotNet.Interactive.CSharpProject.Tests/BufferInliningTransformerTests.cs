// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using File = System.IO.File;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class BufferInliningTransformerTests
{
    [Fact]
    public async Task When_workspace_is_null_then_the_transformer_throw_exception()
    {
        var processor = new BufferInliningTransformer();
        Func<Task> extraction = () => processor.TransformAsync(null);
        await extraction.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Processed_workspace_files_are_modified_inlining_buffers()
    {
        var original = new Workspace(
            files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            },
            buffers: new[]
            {
                new Buffer("Program.cs@alpha", "var newValue = 1000;".EnforceLF())
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(original);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text;

        newCode.Should().NotBe(original.Files.ElementAt(0).Text);
        newCode.Should().Contain("var newValue = 1000;");
        newCode.Should().NotContain("var a = 10;");

        original.Buffers.ElementAt(0).Position.Should().Be(0);
        processed.Buffers.Length.Should().Be(original.Buffers.Length);
        processed.Buffers.ElementAt(0).Position.Should().Be(original.Buffers.ElementAt(0).Position);
        processed.Buffers.ElementAt(0).AbsolutePosition.Should().Be(168);

    }

    [Fact]
    public async Task Buffer_can_be_injected_before_region()
    {
        var original = new Workspace(
            files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            },
            buffers: new[]
            {
                new Buffer("Program.cs@alpha[before]", "var newValue = 1000;".EnforceLF())
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(original);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text;

        newCode.Should().NotBe(original.Files.ElementAt(0).Text);
        newCode.Should().Contain("var newValue = 1000;");
        newCode.Should().Contain("var a = 10;");

        original.Buffers.ElementAt(0).Position.Should().Be(0);
        processed.Buffers.Length.Should().Be(original.Buffers.Length);
        processed.Buffers.ElementAt(0).Position.Should().Be(original.Buffers.ElementAt(0).Position);
        processed.Buffers.ElementAt(0).AbsolutePosition.Should().Be(155);
    }

    [Fact]
    public async Task Buffers_can_be_injected_according_to_injection_points()
    {
        var original = new Workspace(
            files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            },
            buffers: new[]
            {
                new Buffer("Program.cs@alpha[before]", "var before = 1000;".EnforceLF()),
                new Buffer("Program.cs@alpha[after]", "var after = 1000;".EnforceLF()),
                new Buffer("Program.cs@alpha", "var inlined = 1000;".EnforceLF())
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(original);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text;

        newCode.Should().NotBe(original.Files.ElementAt(0).Text);
        newCode.Should().Contain("var before = 1000;");
        newCode.Should().Contain("var inlined = 1000;");
        newCode.Should().Contain("var after = 1000;");

        var beforeBuffer = processed.Buffers.First(b => b.Id.RegionName.Contains("before"));
        beforeBuffer.AbsolutePosition.Should().Be(155);

        var inlinedBuffer = processed.Buffers.First(b => b.Id.RegionName == "alpha");
        inlinedBuffer.AbsolutePosition.Should().Be(188);

        var afterBuffer = processed.Buffers.First(b => b.Id.RegionName.Contains("after"));
        afterBuffer.AbsolutePosition.Should().Be(235);
    }

    [Fact]
    public async Task Buffer_can_be_injected_after_region()
    {
        var original = new Workspace(
            files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            },
            buffers: new[]
            {
                new Buffer("Program.cs@alpha[after]", "var newValue = 1000;".EnforceLF())
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(original);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text;

        newCode.Should().NotBe(original.Files.ElementAt(0).Text);
        newCode.Should().Contain("var newValue = 1000;");
        newCode.Should().Contain("var a = 10;");

        original.Buffers.ElementAt(0).Position.Should().Be(0);
        processed.Buffers.Length.Should().Be(original.Buffers.Length);
        processed.Buffers.ElementAt(0).Position.Should().Be(original.Buffers.ElementAt(0).Position);
        processed.Buffers.ElementAt(0).AbsolutePosition.Should().Be(215);
    }

    [Fact]
    public async Task Processed_workspace_files_are_replaced_by_buffer_when_id_is_just_file_name()
    {
        var ws = new Workspace(
            files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            },
            buffers: new[]
            {
                new Buffer("Program.cs", "var newValue = 1000;", 0)
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(ws);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text;

        newCode.Should().NotBe(ws.Files.ElementAt(0).Text);
        newCode.Should().Be("var newValue = 1000;");

        processed.Buffers.Length.Should().Be(ws.Buffers.Length);
        processed.Buffers.ElementAt(0).Position.Should().Be(0);
    }

    [Fact]
    public async Task Processed_workspace_with_single_buffer_with_empty_id_generates_a_program_file()
    {
        var ws = new Workspace(
            buffers: new[]
            {
                new Buffer("", SourceCodeProvider.ConsoleProgramSingleRegion, 0)
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(ws);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text;
        newCode.Should().Contain(SourceCodeProvider.ConsoleProgramSingleRegion);

    }

    [Fact]
    public async Task If_workspace_contains_with_multiple_buffers_targeting_single_file_generates_a_single_file()
    {
        var expectedCode = @"using System;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
var newValueA = ""as string"";
Console.Write(newValueA);
#endregion

            #region beta
var newValueB = ""as different string"";
Console.Write(newValueA + newValueB);
#endregion
        }
    }
}".EnforceLF();

        var ws = new Workspace(
            files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramMultipleRegions)
            },
            buffers: new[]
            {
                new Buffer(new BufferId("Program.cs", "alpha"), "var newValueA = \"as string\";\nConsole.Write(newValueA);", 0),
                new Buffer(new BufferId("Program.cs", "beta"), "var newValueB = \"as different string\";\nConsole.Write(newValueA + newValueB);", 0)
            });
        var processor = new BufferInliningTransformer();

        var processed = await processor.TransformAsync(ws);
        processed.Should().NotBeNull();
        processed.Files.Should().NotBeEmpty();
        var newCode = processed.Files.ElementAt(0).Text.EnforceLF();
        newCode.Should().Be(expectedCode);
    }

    [Fact]
    public async Task If_workspace_contains_files_whose_names_are_absolute_paths_and_they_have_no_content_then_the_contents_are_read_from_disk()
    {
        using (var directory = DisposableDirectory.Create())
        {
            var filePath = Path.Combine(directory.Directory.FullName, "Program.cs");
            var content =
                @"using System;";
            File.WriteAllText(filePath, content);
            var ws = new Workspace(
                files: new[]
                {
                    new ProjectFileContent(filePath, null)
                }
            );

            var processor = new BufferInliningTransformer();
            var processed = await processor.TransformAsync(ws);
            processed.Files[0].Text.Should().Be(content);
        }
    }

    [Fact]
    public async Task If_workspace_contains_buffers_whose_file_names_are_absolute_paths_the_contents_are_read_from_disk()
    {
        using (var directory = DisposableDirectory.Create())
        {
            var filePath = Path.Combine(directory.Directory.FullName, "Program.cs");
            var fileContent =
                @"using System;
namespace Code{{
    public static class Program{{
        public static void Main(){{
        #region region one
        #endregion
        }}
    }}
}}".EnforceLF();
            var expectedFileContent =
                @"using System;
namespace Code{{
    public static class Program{{
        public static void Main(){{
        #region region one
Console.Write(2);
#endregion
        }}
    }}
}}".EnforceLF();

            File.WriteAllText(filePath, fileContent);
            var ws = new Workspace(
                buffers: new[]
                {
                    new Buffer(new BufferId(filePath,"region one"), "Console.Write(2);"), 
                }
            );

            var processor = new BufferInliningTransformer();
            var processed = await processor.TransformAsync(ws);
            processed.Files[0].Text.EnforceLF().Should().Be(expectedFileContent);
        }
    }
}