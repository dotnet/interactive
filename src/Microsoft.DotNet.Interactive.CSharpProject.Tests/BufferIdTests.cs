// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Recipes;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class BufferIdTests
{
    [TestMethod]
    [DataRow("MyFile.cs", "MyFile.cs", null)]
    [DataRow("MyFile.cs@myregion", "MyFile.cs", "myregion")]
    public void BufferId_Parse_sets_file_name_and_region_name_correctly(
        string input,
        string expectedFileName,
        string expectedRegionName)
    {
        var bufferId = BufferId.Parse(input);

        bufferId.FileName.Should().Be(expectedFileName);
        bufferId.RegionName.Should().Be(expectedRegionName);
    }

    [TestMethod]
    [DataRow("MyFile.cs", "MyFile.cs", true)]
    [DataRow("MyFile.cs@myregion", "MyFile.cs@myregion", true)]
    [DataRow("MyFile.cs", "MyFile.cs@myregion", false)]
    public void BufferIds_are_equal_only_if_they_have_the_same_file_and_region_names(
        string input1,
        string input2,
        bool expectedToBeEqual)
    {
        var bufferId1 = BufferId.Parse(input1);
        var bufferId2 = BufferId.Parse(input2);

        bufferId1.Equals(bufferId2).Should().Be(expectedToBeEqual);
        (bufferId1 == bufferId2).Should().Be(expectedToBeEqual);
        (bufferId2 == bufferId1).Should().Be(expectedToBeEqual);
        (bufferId1 != bufferId2).Should().Be(!expectedToBeEqual);
        (bufferId2 != bufferId1).Should().Be(!expectedToBeEqual);
    }

    [TestMethod]
    [DataRow("MyFile.cs", "MyFile.cs", true)]
    [DataRow("MyFile.cs@myregion", "MyFile.cs@myregion", true)]
    [DataRow("MyFile.cs", "MyFile.cs@myregion", false)]
    public void BufferIds_have_the_same_hash_codes_only_if_they_have_the_same_file_and_region_names(
        string input1,
        string input2,
        bool expectedToBeEqual)
    {
        var bufferId1 = BufferId.Parse(input1);
        var bufferId2 = BufferId.Parse(input2);

        bufferId1.GetHashCode().Equals(bufferId2.GetHashCode()).Should().Be(expectedToBeEqual);
    }

    [TestMethod]
    public void ToString_returns_filename_if_no_region_was_specified()
    {
        var bufferId = new BufferId("MyFile.cs");

        bufferId.ToString().Should().Be("MyFile.cs");
    }

    [TestMethod]
    public void ToString_returns_filename_at_region_if_a_region_was_specified()
    {
        var bufferId = new BufferId("MyFile.cs", "myregion");

        bufferId.ToString().Should().Be("MyFile.cs@myregion");
    }

    [TestMethod]
    public void BufferId_JSON_serializes_as_a_string()
    {
        var json = new { Id = new BufferId("MyFile.cs", "myregion") }.ToJson();

        json.Should().Be(@"{""Id"":""MyFile.cs@myregion""}");
    }

    [TestMethod]
    public void BufferId_JSON_deserializes_from_a_string()
    {
        var thingJson = new 
        {
            Id = "MyFile.cs@myregion"
        }.ToJson();

        var thing = thingJson.FromJsonTo<ThingWithId>();

        thing.Id.FileName.Should().Be("MyFile.cs");
        thing.Id.RegionName.Should().Be("myregion");
    }

    public class ThingWithId
    {
        public BufferId Id { get; set; }
    }
}