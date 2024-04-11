// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.DotNet.PlatformAbstractions;
using Xunit;
using static Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public abstract class DirectoryAccessorTests
{
    public abstract IDirectoryAccessor GetDirectory(DirectoryInfo dirInfo, DirectoryInfo rootDirectoryToAddFiles = null);

    public abstract IDirectoryAccessor CreateDirectory([CallerMemberName]string testName = null);

    [Fact]
    public void It_can_retrieve_all_files_recursively()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);

        var files = directory.GetAllFilesRecursively();

        files.Should()
            .Contain(new RelativeFilePath("BasicConsoleApp.csproj"))
            .And
            .Contain(new RelativeFilePath("Program.cs"))
            .And
            .Contain(new RelativeFilePath("Readme.md"))
            .And
            .Contain(new RelativeFilePath("Subdirectory/AnotherProgram.cs"))
            .And
            .Contain(new RelativeFilePath("Subdirectory/Tutorial.md"));
    }

    [Fact]
    public void It_can_retrieve_all_files_at_root()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);

        var files = directory.GetAllFiles();

        files.Should()
            .Contain(new RelativeFilePath("BasicConsoleApp.csproj"))
            .And
            .Contain(new RelativeFilePath("Program.cs"))
            .And
            .Contain(new RelativeFilePath("Readme.md"))
            .And
            .NotContain(new RelativeFilePath("Subdirectory/AnotherProgram.cs"))
            .And
            .NotContain(new RelativeFilePath("Subdirectory/Tutorial.md"));
    }

    [Fact]
    public void GetAllFilesRecursively_does_not_return_directories()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);

        var files = directory.GetAllFilesRecursively();

        files.Should().NotContain(f => f.Value.EndsWith("Subdirectory"));
        files.Should().NotContain(f => f.Value.EndsWith("Subdirectory/"));
    }

    [Fact]
    public void It_can_retrieve_all_directories_recursively()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);

        var directories = directory.GetAllDirectoriesRecursively();

        directories.Should()
            .Contain(new RelativeDirectoryPath("Subdirectory"));
    }

    [Fact]
    public void GetAllDirectoriesRecursively_does_not_return_files()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);

        var directories = directory.GetAllDirectoriesRecursively();

        directories.Should()
            .NotContain(d => d.Value.EndsWith("BasicConsoleApp.csproj"))
            .And
            .NotContain(d => d.Value.EndsWith("Program.cs"))
            .And
            .NotContain(d => d.Value.EndsWith("Readme.md"))
            .And
            .NotContain(d => d.Value.EndsWith("Subdirectory/AnotherProgram.cs"))
            .And
            .NotContain(d => d.Value.EndsWith("Subdirectory/Tutorial.md"));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("./Subdirectory")]
    public void When_the_directory_exists_DirectoryExists_returns_true(string path)
    {
        var directoryAccessor = GetDirectory(TestAssets.SampleConsole);

        directoryAccessor.DirectoryExists(path).Should().BeTrue();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("Subdirectory")]
    public void It_can_ensure_a_directory_exists(string path)
    {
        var directoryAccessor = CreateDirectory();

        directoryAccessor.EnsureDirectoryExists(path);

        directoryAccessor.DirectoryExists(path).Should().BeTrue();
    }

    [Fact]
    public void EnsureDirectoryExists_is_idempotent()
    {
        var directoryAccessor = CreateDirectory();

        var subdirectory = "./a-subdirectory";

        directoryAccessor.EnsureDirectoryExists(subdirectory);

        directoryAccessor
            .Invoking(d => d.EnsureDirectoryExists(subdirectory))
            .Should()
            .NotThrow();
    }

    [Theory]
    [InlineData("./some-file.txt", "hello!")]
    public void It_can_write_text_to_a_file(string path, string text)
    {
        var directory = CreateDirectory();

        directory.WriteAllText(path, text);

        directory.ReadAllText(path).Should().Be(text);
    }

    [Fact]
    public void It_can_overwrite_an_existing_file()
    {
        var directory = CreateDirectory();

        directory.WriteAllText("./some-file.txt", "original text");
        directory.WriteAllText("./some-file.txt", "updated text");

        directory.ReadAllText("./some-file.txt").Should().Be("updated text");
    }

    [Fact]
    public void When_the_file_exists_FileExists_returns_true()
    {
        var testDir = TestAssets.SampleConsole;
        GetDirectory(testDir).FileExists(new RelativeFilePath("Program.cs")).Should().BeTrue();
    }

    [Fact]
    public void When_the_filepath_is_null_FileExists_returns_false()
    {
        var testDir = TestAssets.SampleConsole;
        GetDirectory(testDir).Invoking(d => d.FileExists(null)).Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(@"Subdirectory/AnotherProgram.cs")]
    [InlineData(@"Subdirectory\AnotherProgram.cs")]
    public void When_the_filepath_contains_subdirectory_paths_FileExists_returns_true(string filepath)
    {
        var testDir = TestAssets.SampleConsole;
        GetDirectory(testDir).FileExists(new RelativeFilePath(filepath)).Should().BeTrue();
    }

    [Theory]
    [InlineData(@"../Program.cs")]
    [InlineData(@"..\Program.cs")]
    public void When_the_filepath_contains_a_path_that_looks_upward_in_tree_then_FileExists_returns_the_text(string filePath)
    {
        var rootDirectoryToAddFiles = TestAssets.SampleConsole;
        var testDir = new DirectoryInfo(Path.Combine(rootDirectoryToAddFiles.FullName, "Subdirectory"));
        GetDirectory(testDir, rootDirectoryToAddFiles).FileExists(new RelativeFilePath(filePath)).Should().BeTrue();
    }

    [Fact]
    public void When_the_filepath_contains_an_existing_file_ReadAllText_returns_the_text()
    {
        var testDir = TestAssets.SampleConsole;
        GetDirectory(testDir).ReadAllText(new RelativeFilePath("Program.cs")).Should().Contain("Hello World!");
    }

    [Theory]
    [InlineData(@"Subdirectory/AnotherProgram.cs")]
    [InlineData(@"Subdirectory\AnotherProgram.cs")]
    public void When_the_filepath_contains_an_existing_file_from_subdirectory_then_ReadAllText_returns_the_text(string filePath)
    {
        var testDir = TestAssets.SampleConsole;
        GetDirectory(testDir).ReadAllText(new RelativeFilePath(filePath)).Should().Contain("Hello from Another Program!");
    }

    [Theory]
    [InlineData(@"../Program.cs")]
    [InlineData(@"..\Program.cs")]
    public void When_the_filepath_contains_a_path_that_looks_upward_in_tree_then_ReadAllText_returns_the_text(string filePath)
    {
        var rootDirectoryToAddFiles = TestAssets.SampleConsole;
        var testDir = new DirectoryInfo(Path.Combine(rootDirectoryToAddFiles.FullName, "Subdirectory"));
        var value = GetDirectory(testDir, rootDirectoryToAddFiles).ReadAllText(new RelativeFilePath(filePath));
        value.Should().Contain("Hello World!");
    }

    [Fact]
    public void Should_return_a_directory_accessor_for_a_relative_path()
    {
        var rootDir = TestAssets.SampleConsole;
        var outerDirAccessor = GetDirectory(rootDir);
        var inner = outerDirAccessor.GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath("Subdirectory"));
        inner.FileExists(new RelativeFilePath("AnotherProgram.cs")).Should().BeTrue();
    }

    [Fact]
    public void Path_separators_are_uniform()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);
        var unexpectedPathSeparator = OperatingSystemPlatform == Platform.Windows
            ? "/"
            : "\\";

        foreach (var relativePath in directory.GetAllFilesRecursively())
        {
            var fullyQualifiedPath = directory.GetFullyQualifiedPath(relativePath).FullName;
            fullyQualifiedPath.Should().NotContain(unexpectedPathSeparator);
        }
    }

    [Fact]
    public void It_can_make_a_directory_accessor_from_an_absolute_DirectoryInfo()
    {
        var directory = GetDirectory(TestAssets.SampleConsole);

        var fullyQualifiedSubdirectory = new DirectoryInfo(directory.GetFullyQualifiedFilePath("./Subdirectory/").FullName);

        var subdirectory = directory.GetDirectoryAccessorFor(fullyQualifiedSubdirectory);

        subdirectory.FileExists("Tutorial.md").Should().BeTrue();
    }
}

public class FileSystemDirectoryAccessorTests : DirectoryAccessorTests
{
    public override IDirectoryAccessor CreateDirectory([CallerMemberName]string testName = null)
    {
        var directory = PrebuildUtilities.CreateDirectory(testName);

        return new FileSystemDirectoryAccessor(directory);
    }

    public override IDirectoryAccessor GetDirectory(DirectoryInfo directoryInfo, DirectoryInfo rootDirectoryToAddFiles = null)
    {
        return new FileSystemDirectoryAccessor(directoryInfo);
    }
}

public class InMemoryDirectoryAccessorTests : DirectoryAccessorTests
{
    public override IDirectoryAccessor CreateDirectory([CallerMemberName]string testName = null)
    {
        return new InMemoryDirectoryAccessor();
    }

    [Theory]
    [InlineData("one")]
    [InlineData("./one")]
    [InlineData("./one/two")]
    [InlineData("./one/two/three")]
    public void DirectoryExists_returns_true_for_parent_directories_of_explicitly_added_relative_file_paths(string relativeDirectoryPath)
    {
        var directory = new InMemoryDirectoryAccessor
        {
            ("./one/two/three/file.txt", "")
        };

        directory.DirectoryExists(relativeDirectoryPath).Should().BeTrue();
    }

    public override IDirectoryAccessor GetDirectory(DirectoryInfo rootDirectory, DirectoryInfo rootDirectoryToAddFiles = null)
    {
        return new InMemoryDirectoryAccessor(rootDirectory, rootDirectoryToAddFiles)
        {
            ("BasicConsoleApp.csproj",
                @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.1</TargetFramework>
  </PropertyGroup>

</Project>
"),
            ("Program.cs",
                @"using System;

namespace BasicConsoleApp
{
    class Program
    {
        static void MyProgram(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}"),
            ("Readme.md",
                @"This is a sample *markdown file*

```cs Program.cs
```"),
            ("./Subdirectory/Tutorial.md", "This is a sample *tutorial file*"),
            ("./Subdirectory/AnotherProgram.cs",
                @"using System;
using System.Collections.Generic;
using System.Text;

namespace TestProjects.BasicConsoleApp.Subdirectory
{
    class AnotherPorgram
    {
        static void MyAnotherProgram(string[] args)
        {
            Console.WriteLine(""Hello from Another Program!"");
        }
    }
    }
")
        };
    }
}