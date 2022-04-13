// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class SignatureHelpItemTests
    {
        [Theory]
        [InlineData(
@"{
  ""name"": ""Write"",
  ""label"": ""void Console.Write(bool value)"",
  ""documentation"": ""Writes the text representation of the specified Boolean value to the standard output stream."",
  ""parameters"": [
    {
      ""name"": ""value"",
      ""label"": ""bool value"",
      ""documentation"": ""**value**: The value to write.""
    }
  ]
}")]
        public void CanDeserializeFromJson(string source)
        {
            var si = JsonConvert.DeserializeObject<SignatureHelpItem>(source);
            si.Documentation.Should().NotBeNull();
            si.Documentation.Should().Be("Writes the text representation of the specified Boolean value to the standard output stream.");

            si.Parameters.Should().NotBeNullOrEmpty();
            si.Parameters.First().Documentation.Should().Be("**value**: The value to write.");
        }
    }
}