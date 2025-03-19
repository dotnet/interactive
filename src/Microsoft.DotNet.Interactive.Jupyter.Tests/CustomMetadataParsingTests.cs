// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

[TestClass]
public class CustomMetadataParsingTests
{
    [TestMethod]
    public void Input_cell_metadata_can_be_parsed_from_dotnet_interactive_metadata()
    {
        var rawMetadata = new
        {
            // the value specified is `language`, but in reality this was the kernel name
            dotnet_interactive = new InputCellMetadata(language: "fsharp")
        };
        var rawMetadataJson = JsonSerializer.Serialize(rawMetadata);
        var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
        metadata.Should()
            .ContainKey("dotnet_interactive")
            .WhoseValue
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(new InputCellMetadata(language: "fsharp"));
    }

    [TestMethod]
    public void Input_cell_metadata_can_be_parsed_from_polyglot_notebook_metadata()
    {
        var rawMetadata = new
        {
            polyglot_notebook = new InputCellMetadata(kernelName: "fsharp")
        };
        var rawMetadataJson = JsonSerializer.Serialize(rawMetadata);
        var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
        metadata.Should()
            .ContainKey("polyglot_notebook")
            .WhoseValue
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(new InputCellMetadata(kernelName: "fsharp"));
    }

    [TestMethod]
    public void Input_cell_metadata_can_be_parsed_with_no_fields()
    {
        var rawMetadata = new
        {
            dotnet_interactive = new InputCellMetadata()
        };
        var rawMetadataJson = JsonSerializer.Serialize(rawMetadata);
        var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
        metadata.Should()
            .ContainKey("dotnet_interactive")
            .WhoseValue
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(new InputCellMetadata());
    }

    [TestMethod]
    public void Input_cell_metadata_is_not_parsed_when_not_present()
    {
        var rawMetadata = new
        {
            dotnet_interactive_but_not_the_right_shape = new InputCellMetadata(language: "fsharp")
        };
        var rawMetadataJson = JsonSerializer.Serialize(rawMetadata);
        var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
        metadata.Should()
            .NotContainKey("dotnet_interactive");
    }
}