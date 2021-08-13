﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class CustomMetadataParsingTests
    {
        [Fact]
        public void Input_cell_metadata_can_be_parsed_with_all_fields()
        {
            var rawMetadata = new
            {
                dotnet_interactive = new InputCellMetadata("fsharp")
            };
            var rawMetadataJson = JsonSerializer.Serialize(rawMetadata);
            var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
            metadata.Should()
                .ContainKey("dotnet_interactive")
                .WhichValue
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new InputCellMetadata("fsharp"));
        }

        [Fact]
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
                .WhichValue
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new InputCellMetadata() );
        }

        [Fact]
        public void Input_cell_metadata_is_not_parsed_when_not_present()
        {
            var rawMetadata = new
            {
                dotnet_interactive_but_not_the_right_shape = new InputCellMetadata("fsharp")
            };
            var rawMetadataJson = JsonSerializer.Serialize(rawMetadata);
            var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
            metadata.Should()
                .NotContainKey("dotnet_interactive");
        }
    }
}
