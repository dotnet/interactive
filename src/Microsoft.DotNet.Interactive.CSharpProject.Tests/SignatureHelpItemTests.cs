// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class SignatureHelpItemTests
{
    [TestMethod]
    [DataRow(
        @"{
  ""name"": ""Write"",
  ""label"": ""void Console.Write(bool value)"",
  ""documentation"": {
    ""value"": ""Writes the text representation of the specified Boolean value to the standard output stream."",
    ""mimeType"": ""text/markdown""
  },
  ""parameters"": [
    {
      ""label"": ""bool value"",
      ""documentation"": {
        ""value"": ""**value**: The value to write."",
        ""mimeType"": ""text/markdown""
      }
    }
  ]
}")]
    public void CanDeserializeFromJson(string source)
    {
        var si = JsonConvert.DeserializeObject<SignatureInformation>(source);
        si.Documentation.Should().NotBeNull();
        si.Documentation.Value.Should().Be("Writes the text representation of the specified Boolean value to the standard output stream.");
        si.Documentation.MimeType.Should().Be("text/markdown");

        si.Parameters.Should().NotBeNullOrEmpty();
        si.Parameters.First().Documentation.Value.Should().Be("**value**: The value to write.");
        si.Parameters.First().Documentation.MimeType.Should().Be("text/markdown");
    }
}