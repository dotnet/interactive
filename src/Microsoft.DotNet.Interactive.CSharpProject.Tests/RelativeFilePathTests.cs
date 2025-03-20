// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class RelativeFilePathTests
{
    private readonly TestContext _output;

    public RelativeFilePathTests(TestContext output)
    {
        _output = output;
    }

    [TestMethod]
    public void Can_create_file_paths_from_string_with_directory()
    {
        var path = new RelativeFilePath("../readme.md");
        path.Value.Should().Be("../readme.md");    
    }

    [TestMethod]
    public void Can_create_file_paths_from_string_without_directory()
    {
        var path = new RelativeFilePath("readme.md");
        path.Value.Should().Be("./readme.md");
    }

    [TestMethod]
    public void Normalises_the_passed_path()
    {
        var path = new RelativeFilePath(@"..\readme.md");
        _output.WriteLine(path.Value);
        _output.WriteLine(path.Directory.Value);
        path.Value.Should().Be("../readme.md");
    }

    [TestMethod]
    public void Throws_exception_if_the_path_contains_invalid_filename_characters()
    {
        Action action = () => new RelativeFilePath(@"abc*def");
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Throws_exception_if_the_path_contains_invalid_path_characters()
    {
        Action action = () => new RelativeFilePath(@"abc|def");
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Throws_exception_if_the_path_is_empty()
    {
        Action action = () => new RelativeFilePath("");
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("../src/Program.cs", "../src/")]
    [DataRow("src/Program.cs", "./src/")]
    [DataRow("Readme.md", "./")]
    public void Returns_the_directory_path(string path, string directory)
    {
        var relativePath = new RelativeFilePath(path);
        relativePath.Directory.Value.Should().Be(directory);
    }

    [TestMethod]
    public void Extension_returns_file_extension_with_a_dot()
    {
        new RelativeFilePath("../Program.cs").Extension.Should().Be(".cs");
    }

    [TestMethod]
    [DataRow("Program.cs", "Program.cs")]
    [DataRow("./Program.cs", "Program.cs")]
    [DataRow("./Program.cs", @".\Program.cs")]
    [DataRow("./a/Program.cs", @".\a/Program.cs")]
    public void Equality_is_based_on_same_resolved_file_path(
        string value1,
        string value2)
    {
        var path1 = new RelativeFilePath(value1);
        var path2 = new RelativeFilePath(value2);

        _output.WriteLine($"path1.Value: {path1.Value}");
        _output.WriteLine($"path1.Directory.Value: {path1.Directory.Value}");
        _output.WriteLine($"path2.Value: {path2.Value}");
        _output.WriteLine($"path2.Directory.Value: {path2.Directory.Value}");

        path1.GetHashCode().Should().Be(path2.GetHashCode());
        path1.Equals(path2).Should().BeTrue();
        path2.Equals(path1).Should().BeTrue();
    }
}