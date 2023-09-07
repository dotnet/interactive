// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class BufferExtractorTests
{
    [Fact]
    public void it_generates_a_buffer_only_workspace_when_no_regions_are_defined()
    {
        var noRegionFiles = new[]
        {
            new ProjectFileContent("buffer1.cs", SourceCodeProvider.CodeWithNoRegions),
            new ProjectFileContent("buffer2.cs", SourceCodeProvider.CodeWithNoRegions),
        };

        var transformer = new BufferFromRegionExtractor();
        var result = transformer.Extract(noRegionFiles, workspaceType: "console");
        result.Should().NotBeNull();
        result.Buffers.Should().Contain(found => found.Id == "buffer1.cs" && found.Content == SourceCodeProvider.CodeWithNoRegions);
        result.Buffers.Should().Contain(found => found.Id == "buffer2.cs" && found.Content == SourceCodeProvider.CodeWithNoRegions);
        result.Files.Should().BeNullOrEmpty();
    }

    [Fact]
    public void it_generates_a_file_and_buffers_workspace()
    {
        var noRegionFiles = new[]
        {
            new ProjectFileContent("buffer1.cs", SourceCodeProvider.CodeWithTwoRegions),
            new ProjectFileContent("buffer2.cs", SourceCodeProvider.CodeWithNoRegions),
        };

        var transformer = new BufferFromRegionExtractor();
        var result = transformer.Extract(noRegionFiles, workspaceType: "console");
        result.Should().NotBeNull();

        result.Buffers.Should().Contain(found => found.Id == "buffer1.cs@objetConstruction" && found.Content.EnforceLF() == @"var simpleObject = new JObject
            {
                {""property"", 4}
            };".EnforceLF());
        result.Buffers.Should().Contain(found => found.Id == "buffer1.cs@workspaceIdentifier" && found.Content == @"Console.WriteLine(""jsonDotNet workspace"");");
        result.Buffers.Should().Contain(found => found.Id == "buffer2.cs" && found.Content == SourceCodeProvider.CodeWithNoRegions);
        result.Files.Should().NotBeNullOrEmpty();
        result.Files.Should().Contain(found => found.Name == "buffer1.cs" && found.Text == SourceCodeProvider.CodeWithTwoRegions);
    }

    [Fact]
    public void it_generates_content_with_correct_indentation()
    {
        const string expectedCode = "// Instant represents time from epoch\nInstant now = SystemClock.Instance.GetCurrentInstant();\nConsole.WriteLine($\"now: {now}\");\n\n// Convert an instant to a ZonedDateTime\nZonedDateTime nowInIsoUtc = now.InUtc();\nConsole.WriteLine($\"nowInIsoUtc: {nowInIsoUtc}\");\n\n// Create a duration\nDuration duration = Duration.FromMinutes(3);\nConsole.WriteLine($\"duration: {duration}\");\n\n// Add it to our ZonedDateTime\nZonedDateTime thenInIsoUtc = nowInIsoUtc + duration;\nConsole.WriteLine($\"thenInIsoUtc: {thenInIsoUtc}\");\n\n// Time zone support (multiple providers)\nvar london = DateTimeZoneProviders.Tzdb[\"Europe/London\"];\nConsole.WriteLine($\"london: {london}\");\n\n// Time zone conversions\nvar localDate = new LocalDateTime(2012, 3, 27, 0, 45, 00);\nvar before = london.AtStrictly(localDate);\nConsole.WriteLine($\"before: {before}\");";

        var files = new[]
        {
            new ProjectFileContent("buffer1.cs", SourceCodeProvider.GistWithRegion),
        };

        var transformer = new BufferFromRegionExtractor();
        var result = transformer.Extract(files, workspaceType: "console");
        result.Should().NotBeNull();

        result.Buffers.Should().Contain(found => found.Id == "buffer1.cs@fragment");

        result.Buffers.First().Content.Replace("\r\n", "\n").Should().Be(expectedCode);
    }
}