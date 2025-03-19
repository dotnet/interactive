// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class CompletionItemTests
{
    [TestMethod]
    [DataRow(
        @"{
  ""displayText"": ""BackgroundColor"",
  ""kind"": ""Property"",
  ""filterText"": ""BackgroundColor"",
  ""sortText"": ""BackgroundColor"",
  ""insertText"": ""BackgroundColor"",
  ""documentation"": ""Gets or sets the background color of the console.""
}")]
    public void CanDeserializeFromJson(string source)
    {
        var ci = JsonConvert.DeserializeObject<CompletionItem>(source);
        ci.Documentation.Should().NotBeNull();
        ci.Documentation.Should().Be("Gets or sets the background color of the console.");
    }
}