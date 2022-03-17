// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class MarkdownStringTests

    {
        [Fact]
        public void CanDeserializeFromJson()
        {
            var source = "{\r\n  \"value\": \"Gets or sets the background color of the console.\",\r\n   \"isTrusted\": false\r\n    }";
            var ms = JsonConvert.DeserializeObject<MarkdownString>(source);
            ms.IsTrusted.Should().BeFalse();
            ms.Value.Should().Be("Gets or sets the background color of the console.");
        }

        [Fact]
        public void CanBeAssignedFromString()
        {
            var source = "Gets or sets the background color of the console.";
            MarkdownString ms = source;
            ms.IsTrusted.Should().BeFalse();
            ms.Value.Should().Be("Gets or sets the background color of the console.");
        }
    }
}
