// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.ML;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class DataFrameParserTests
    {
        [Fact]
        public void it_builds_a_DataFrame_parsing_string()
        {
            var text = @"Data Value, Data Label
12, twelve
11, eleven";
            var dataFrame = DataFrameParser.Parse(text);
            dataFrame[0, 0].Should().Be(12);
        }
    }
}