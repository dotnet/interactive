// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Microsoft.DotNet.Interactive.Notebook;
using Newtonsoft.Json;
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
                dotnet_interactive = new InputCellMetadata() { Language = "fsharp" }
            };
            var rawMetadataJson = JsonConvert.SerializeObject(rawMetadata);
            var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
            metadata.Should()
                .ContainKey("dotnet_interactive")
                .WhichValue
                .Should()
                .BeEquivalentTo(new InputCellMetadata() { Language = "fsharp" });
        }

        [Fact]
        public void Input_cell_metadata_can_be_parsed_with_no_fields()
        {
            var rawMetadata = new
            {
                dotnet_interactive = new InputCellMetadata()
            };
            var rawMetadataJson = JsonConvert.SerializeObject(rawMetadata);
            var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
            metadata.Should()
                .ContainKey("dotnet_interactive")
                .WhichValue
                .Should()
                .BeEquivalentTo(new InputCellMetadata() { Language = null });
        }

        [Fact]
        public void Input_cell_metadata_is_not_parsed_when_not_present()
        {
            var rawMetadata = new
            {
                dotnet_interactive_but_not_the_right_shape = new InputCellMetadata() { Language = "fsharp" }
            };
            var rawMetadataJson = JsonConvert.SerializeObject(rawMetadata);
            var metadata = MetadataExtensions.DeserializeMetadataFromJsonString(rawMetadataJson);
            metadata.Should()
                .NotContainKey("dotnet_interactive");
        }
    }
}
