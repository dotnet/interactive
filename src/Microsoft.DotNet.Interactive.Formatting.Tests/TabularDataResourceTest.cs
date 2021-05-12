// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

using Assent;

using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class TabularDataResourceTest
    {
        private readonly Configuration _configuration;

        public TabularDataResourceTest()
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public void can_create_from_document()
        {
            var doc = JsonDocument.Parse(@"[
{ ""name"": ""mitch"", ""age"": 42, ""salary"":10.0, ""active"":true }
]");
            var data = doc.ToTabularDataResource();
            var formattedData = data.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formattedData, _configuration);
        }
    }
}