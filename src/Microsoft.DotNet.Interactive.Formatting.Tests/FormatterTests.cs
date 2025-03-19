// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

[TestClass]
public sealed partial class FormatterTests : FormatterTestBase
{
    [TestMethod]
    public void ListExpansionLimit_can_be_specified_per_type()
    {
        Formatter<KeyValuePair<string, int>>.ListExpansionLimit = 1000;
        Formatter.ListExpansionLimit = 4;
        var dictionary = new Dictionary<string, int>
        {
            { "zero", 0 },
            { "two", 2 },
            { "three", 3 },
            { "four", 4 },
            { "five", 5 },
            { "six", 6 },
            { "seven", 7 },
            { "eight", 8 },
            { "nine", 9 },
            { "ninety-nine", 99 }
        };

        var output = dictionary.ToDisplayString();

        output.Should().Contain("zero");
        output.Should().Contain("0");
        output.Should().Contain("ninety-nine");
        output.Should().Contain("99");
    }
}