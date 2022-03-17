// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
//using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class CSharpProjectKernelCommandOrderTests
    {
//        [Fact]
//        public async Task OpenDocument_before_OpenProject_fails()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandFailed>()
//                .Which
//                .Message
//                .Should()
//                .Contain($"Cannot open document before project has been opened.  Send the command '{nameof(OpenProject)}' first.");
//        }

//        [Fact]
//        public async Task OpenDocument_with_an_existing_file_path_succeeds()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// content") })));
//            var result = await kernel.SendAsync(new OpenDocument("Program.cs"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandSucceeded>();
//        }

//        [Fact]
//        public async Task OpenDocument_with_a_non_existing_file_path_succeeds()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("A_file_that_is_not_part_of_the_default_project.cs", "// content") })));
//            var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandSucceeded>();
//        }

//        [Fact]
//        public async Task OpenDocument_with_path_and_region_name_succeeds()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
//public class Program
//{
//    public static void Main(string[] args)
//    {
//        #region TEST_REGION
//        #endregion
//    }
//}") })));
//            var result = await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandSucceeded>();
//        }

//        [Fact]
//        public async Task OpenDocument_with_region_name_fails_if_ProjectFile_cannot_be_found()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// content") })));
//            var result = await kernel.SendAsync(new OpenDocument("File_that_is_not_part_of_the_project.cs", regionName: "NOT_A_REGION_IN_THE_FILE"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandFailed>()
//                .Which
//                .Message
//                .Should()
//                .Contain("File 'File_that_is_not_part_of_the_project.cs' not found");
//        }

//        [Fact]
//        public async Task OpenDocument_with_region_name_fails_if_region_name_cannot_be_found()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
//public class Program
//{
//    public static void Main(string[] args)
//    {
//        #region TEST_REGION
//        #endregion
//    }
//}") })));
//            var result = await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "NOT_A_REGION_IN_THE_FILE"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandFailed>()
//                .Which
//                .Message
//                .Should()
//                .Contain("Region 'NOT_A_REGION_IN_THE_FILE' not found in file 'Program.cs'");
//        }

//        [Fact]
//        public async Task CompileProject_before_OpenDocument_fails()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            var result = await kernel.SendAsync(new CompileProject());
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandFailed>()
//                .Which
//                .Message
//                .Should()
//                .Contain($"Cannot compile project before document has been opened.  Send the command {nameof(OpenDocument)} first");
//        }

//        [Fact]
//        public async Task RequestHoverText_before_OpenDocument_fails()
//        {
//            var markedCode = @"in$$t x = 1;";
//            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
//            var kernel = new CSharpProjectKernel("csharp");
//            var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, character)));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<CommandFailed>()
//                .Which
//                .Message
//                .Should()
//                .Contain($"Cannot provide hover text before document has been opened.  Send the command {nameof(OpenDocument)} first");
//        }

//        [Fact]
//        public async Task HoverText_is_returned_when_the_entire_file_contents_are_set()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// this content will be replaced") })));
//            await kernel.SendAsync(new OpenDocument("Program.cs"));

//            var markedCode = @"
//public class Program
//{
//    public static void Main(string[] args)
//    {
//        in$$t x = 1;
//    }
//}";
//            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
//            var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, character)));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<HoverTextProduced>()
//                .Which
//                .Content
//                .Should()
//                .Contain(c => c.Value.Contains("System.Int32"));
//        }

//        [Fact]
//        public async Task HoverText_is_returned_when_just_a_region_is_set()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
//public class Program
//{
//    public static void Main(string[] args)
//    {
//        #region TEST_REGION
//        #region
//    }
//}
//") })));
//            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));

//            var markedCode = @"in$$t x = 1;";
//            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
//            var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, character)));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<HoverTextProduced>()
//                .Which
//                .Content
//                .Should()
//                .Contain(c => c.Value.Contains("System.Int32"));
//        }

//        [Fact]
//        public async Task CompileProject_with_no_region_returns_an_assembly()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// this will be wholly replaced") })));
//            await kernel.SendAsync(new OpenDocument("Program.cs"));
//            var result = await kernel.SendAsync(new CompileProject(@"
//public class Program
//{
//    public static void Main(string[] args)
//    {
//    }
//}"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<AssemblyProduced>();
//        }

//        [Fact]
//        public async Task CompileProject_with_a_region_returns_an_assembly()
//        {
//            var kernel = new CSharpProjectKernel("csharp");
//            await kernel.SendAsync(new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
//public class Program
//{
//    public static void Main(string[] args)
//    {
//        #region TEST_REGION
//        #endregion
//    }
//}") })));
//            await kernel.SendAsync(new OpenDocument("Program.cs", regionName: "TEST_REGION"));
//            var result = await kernel.SendAsync(new CompileProject("int x = 1;"));
//            var kernelEvents = result.KernelEvents.ToSubscribedList();
//            kernelEvents
//                .Should()
//                .ContainSingle<AssemblyProduced>();
//        }
    }
}
