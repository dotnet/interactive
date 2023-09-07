// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class RelativeDirectoryPathTests
{
    [Fact]
    public void Can_create_directory_paths_from_string_with_directory()
    {
        var path = new RelativeDirectoryPath("../src");
        path.Value.Should().Be("../src/");
    }

    [Fact]
    public void Normalises_the_passed_path()
    {
        var path = new RelativeDirectoryPath(@"..\src");
        path.Value.Should().Be("../src/");
    }

    [Fact]
    public void Throws_exception_if_the_path_contains_invalid_path_characters()
    {
        Action action = () => new RelativeDirectoryPath(@"abc|def");
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/some/path")]
    [InlineData(@"c:\some\path")]
    [InlineData(@"\\some\path")]
    public void Throws_if_path_is_absolute(string value)
    {
        Action action = () => new RelativeDirectoryPath(value);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(".", ".")]
    [InlineData(".", "./")]
    [InlineData(".", @".\")]
    [InlineData("./", @".\")]
    [InlineData("..", "..")]
    [InlineData(@"../", @"..\")]
    [InlineData("../a/", "../a")]
    [InlineData("a", "./a")]
    public void Equality_is_based_on_same_resolved_directory_path(
        string value1,
        string value2)
    {
        var path1 = new RelativeDirectoryPath(value1);
        var path2 = new RelativeDirectoryPath(value2);

        path1.GetHashCode().Should().Be(path2.GetHashCode());
        path1.Equals(path2).Should().BeTrue();
        path2.Equals(path1).Should().BeTrue();
    }
}